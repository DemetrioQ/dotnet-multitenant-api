using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

public abstract class IntegrationTestBase
{
    protected readonly HttpClient Client;

    protected IntegrationTestBase(WebAppFactory factory)
    {
        Client = factory.CreateClient();
    }

    protected async Task<string> GetAuthTokenAsync(string tenantId, string email = "test@test.com", string password = "Password1!")
    {
        // Register
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId,
            email,
            password
        });

        // Login
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            tenantId,
            email,
            password
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return result!.JwtToken;
    }

    protected void SetAuthHeader(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private record LoginResult(string JwtToken, string RefreshToken, DateTime ExpiresAt);
}

