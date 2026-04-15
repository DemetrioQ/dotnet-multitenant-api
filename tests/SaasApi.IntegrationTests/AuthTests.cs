using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class AuthTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_Returns201()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co", slug = "auth-co" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "user@authco.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 2", slug = "auth-co-2" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "dupe@authco.com",
            password = "Password1!"
        });

        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant.TenantId,
            email = "dupe@authco.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 3", slug = "auth-co-3" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "login@authco.com",
            password = "Password1!"
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            tenantId = tenant.TenantId,
            email = "login@authco.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 4", slug = "auth-co-4" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "valid@authco.com",
            password = "Password1!"
        });

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            tenantId = tenant.TenantId,
            email = "valid@authco.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body!.JwtToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_ReturnsNewToken()
    {
        // Arrange — create tenant, register, login to get tokens
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 5", slug = "auth-co-5" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "refresh@authco.com",
            password = "Password1!"
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            tenantId = tenant.TenantId,
            email = "refresh@authco.com",
            password = "Password1!"
        });

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();

        // Act — exchange the refresh token
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login!.RefreshToken,
            tenantId = tenant.TenantId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body!.JwtToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBe(login.RefreshToken); // new token issued
    }
    private record TenantResult(Guid TenantId);
    private record LoginResult(string JwtToken, string RefreshToken, DateTime ExpiresAt);
}
