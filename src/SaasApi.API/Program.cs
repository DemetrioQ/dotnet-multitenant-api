using Microsoft.EntityFrameworkCore;
using SaasApi.API.Extensions;
using SaasApi.API.Middleware;
using SaasApi.API.OpenApi;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

builder.Services
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddRateLimiting();

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("CustomerOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("sub_type", "customer"));
});

builder.Services.AddControllers().AddJsonOptions(opts =>
{
    // Accept and emit enums as their string names (case-insensitive) — so clients can
    // send { "role": "admin" } instead of { "role": 1 } and responses are readable.
    opts.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter(
            namingPolicy: null, allowIntegerValues: false));
});
builder.Services.AddOpenApi(opts =>
{
    opts.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    opts.AddDocumentTransformer<ApiVersionPathTransformer>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var frontendUrl = builder.Configuration["App:FrontendUrl"]?.TrimEnd('/');
        var storefrontSuffix = builder.Configuration["Storefront:HostSuffix"];

        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

                if (uri.Host == "localhost") return true;

                if (!string.IsNullOrEmpty(frontendUrl) &&
                    string.Equals(origin.TrimEnd('/'), frontendUrl, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!string.IsNullOrEmpty(storefrontSuffix) &&
                    uri.Host.EndsWith(storefrontSuffix, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(opts =>
{
    opts.Title = "SaaS API";
    opts.Theme = ScalarTheme.Purple;
    opts.DefaultHttpClient = new(ScalarTarget.JavaScript, ScalarClient.Fetch);
});

app.MapGet("/", () => Results.Redirect("/scalar/v1", permanent: false))
   .ExcludeFromDescription();

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseHttpsRedirection();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (tenantId is not null) diagnosticContext.Set("TenantId", tenantId);
        if (userId is not null) diagnosticContext.Set("UserId", userId);
    };
});

if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

app.UseAuthentication();
// Tenant resolution must come AFTER authentication (needs validated JWT)
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();

    // Apply pending EF migrations on startup. Single-replica deploy on the Oracle VM,
    // so the textbook race-condition concerns about concurrent migrators don't apply.
    // A bad migration here will fail-fast at boot rather than serving broken data.
    await db.Database.MigrateAsync();

    var passwordHasher = scope.ServiceProvider.GetRequiredService<SaasApi.Application.Common.Interfaces.IPasswordHasher>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await SaasApi.Infrastructure.Persistence.DatabaseSeeder.SeedAsync(db, passwordHasher, configuration);
}

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
