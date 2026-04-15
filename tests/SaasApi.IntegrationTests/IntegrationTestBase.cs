using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Infrastructure.Persistence;
using SaasApi.IntegrationTests;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public abstract class IntegrationTestBase
{
    protected readonly HttpClient Client;
    protected readonly WebAppFactory Factory;  // <-- expose factory

    protected IntegrationTestBase(WebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<string> GetAuthTokenAsync(HttpClient client, Guid tenantId, string email = "test@test.com", string password = "Password1!")
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        await client.PostAsJsonAsync("/api/auth/register", new { email, password });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return result!.JwtToken;
    }

    protected async Task<string> CreateAdminAsync(Guid tenantId, string email, string password = "Password1!")
    {
        Client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());

        await Client.PostAsJsonAsync("/api/auth/register", new { email, password });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == email);
        user.UpdateRole("admin");
        await db.SaveChangesAsync();

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return result!.JwtToken;
    }

    protected void SetTenantContext(HttpClient client, string token, Guid tenantId)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
    }

    private record LoginResult(string JwtToken, string RefreshToken, DateTime ExpiresAt);
}