using Asp.Versioning;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SaasApi.API.Middleware;
using SaasApi.Application.Common.Behaviors;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Settings;
using SaasApi.Domain.Interfaces;
using SaasApi.Infrastructure.Persistence;
using SaasApi.Infrastructure.Repositories;
using SaasApi.Infrastructure.Services;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace SaasApi.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(SaasApi.Application.Common.Behaviors.ValidationBehavior<,>).Assembly));

        services.AddValidatorsFromAssembly(
            typeof(SaasApi.Application.Common.Behaviors.ValidationBehavior<,>).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddMemoryCache();
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiVersioning(opts =>
        {
            opts.DefaultApiVersion = new ApiVersion(1, 0);
            opts.AssumeDefaultVersionWhenUnspecified = true;
            opts.ReportApiVersions = true;
            opts.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentCustomerService, CurrentCustomerService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IStoreUrlBuilder, StoreUrlBuilder>();
        services.AddScoped<IEmailTemplateRenderer, SaasApi.Infrastructure.Services.Email.EmailTemplateRenderer>();

        var resendApiKey = config["Resend:ApiKey"];
        if (!string.IsNullOrWhiteSpace(resendApiKey))
        {
            services.Configure<ResendSettings>(config.GetSection("Resend"));
            services.AddHttpClient<IEmailService, ResendEmailService>(client =>
            {
                client.BaseAddress = new Uri("https://api.resend.com/");
            });
        }
        else
        {
            services.AddScoped<IEmailService, EmailService>();
        }

        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
        services.AddHostedService<BackgroundJobProcessor>();

        var paymentsProvider = (config["Payments:Provider"] ?? "simulation").ToLowerInvariant();
        if (paymentsProvider == "stripe")
            services.AddScoped<IPaymentService, SaasApi.Infrastructure.Services.Payments.StripePaymentService>();
        else
            services.AddScoped<IPaymentService, SaasApi.Infrastructure.Services.Payments.SimulationPaymentService>();

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global limiter: 100 requests / 60 seconds, partitioned by authenticated user or IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var partitionKey = userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromSeconds(60),
                    QueueLimit = 0
                });
            });

            // Stricter limiter for auth endpoints: 10 requests / 60 seconds per IP
            options.AddSlidingWindowLimiter("AuthRateLimit", limiter =>
            {
                limiter.Window = TimeSpan.FromSeconds(60);
                limiter.PermitLimit = 10;
                limiter.SegmentsPerWindow = 6;
                limiter.QueueLimit = 0;
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                var problem = new ProblemDetails
                {
                    Title = "Too many requests",
                    Detail = "You have exceeded the rate limit. Please try again later.",
                    Status = StatusCodes.Status429TooManyRequests
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problem, ct);
            };
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)),
                    RoleClaimType = ClaimTypes.Role,
                };
            });

        return services;
    }
}
