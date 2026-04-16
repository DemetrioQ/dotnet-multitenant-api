using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Infrastructure.Persistence;
using SaasApi.IntegrationTests;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public abstract class IntegrationTestBase
{
    protected readonly HttpClient Client;
    protected readonly WebAppFactory Factory;

    protected IntegrationTestBase(WebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<string> GetAuthTokenAsync(HttpClient client, Guid tenantId, string slug, string email = "test@test.com", string password = "Password1!")
    {
        await client.PostAsJsonAsync("/api/auth/register", new { tenantId, email, password });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { slug, email, password });
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return result!.JwtToken;
    }

    protected async Task<string> CreateAdminAsync(Guid tenantId, string slug, string email, string password = "Password1!")
    {
        await Client.PostAsJsonAsync("/api/auth/register", new { tenantId, email, password });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == email);
        user.UpdateRole("admin");
        await db.SaveChangesAsync();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { slug, email, password });
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return result!.JwtToken;
    }

    protected void SetTenantContext(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private record LoginResult(string JwtToken, DateTime ExpiresAt);
}
