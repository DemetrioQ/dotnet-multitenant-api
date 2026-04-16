using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            slug = "auth-co-3",
            email = "login@authco.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndCookie()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 4", slug = "auth-co-4" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "valid@authco.com",
            password = "Password1!"
        });
        await VerifyUserEmailAsync("valid@authco.com");

        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            slug = "auth-co-4",
            email = "valid@authco.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body!.JwtToken.Should().NotBeNullOrEmpty();

        // Refresh token must be in HttpOnly cookie, not in the response body
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains("refreshToken") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RefreshToken_ReturnsNewToken()
    {
        // Arrange — create tenant, register, login (cookie stored automatically by CookieContainerHandler)
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 5", slug = "auth-co-5" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "refresh@authco.com",
            password = "Password1!"
        });
        await VerifyUserEmailAsync("refresh@authco.com");

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            slug = "auth-co-5",
            email = "refresh@authco.com",
            password = "Password1!"
        });

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();

        // Act — call refresh with no body; cookie is sent automatically
        var response = await Client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body!.JwtToken.Should().NotBeNullOrEmpty();

        // A new refresh token cookie must have been issued (rotation)
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains("refreshToken"));
    }

    [Fact]
    public async Task Logout_ClearsRefreshTokenCookie()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 6", slug = "auth-co-6" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "logout@authco.com",
            password = "Password1!"
        });

        await Client.PostAsJsonAsync("/api/auth/login", new
        {
            slug = "auth-co-6",
            email = "logout@authco.com",
            password = "Password1!"
        });

        var response = await Client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // After logout the refresh cookie should be expired/cleared
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains("refreshToken") && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_UnverifiedEmail_Returns401()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 7", slug = "auth-co-7" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "unverified@authco.com",
            password = "Password1!"
        });

        // Attempt login without verifying email first
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            slug = "auth-co-7",
            email = "unverified@authco.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_Returns200()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 8", slug = "auth-co-8" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "toverify@authco.com",
            password = "Password1!"
        });

        // Get the token directly from DB
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "toverify@authco.com");
        var token = user.EmailVerificationToken;

        var response = await Client.GetAsync($"/api/auth/verify-email?token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyEmail_ThenLogin_Succeeds()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Auth Co 9", slug = "auth-co-9" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email = "fullflow@authco.com",
            password = "Password1!"
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "fullflow@authco.com");
        var token = user.EmailVerificationToken;

        await Client.GetAsync($"/api/auth/verify-email?token={token}");

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            slug = "auth-co-9",
            email = "fullflow@authco.com",
            password = "Password1!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns400()
    {
        var response = await Client.GetAsync("/api/auth/verify-email?token=invalid-token-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task VerifyUserEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == email);
        user.VerifyEmail();
        await db.SaveChangesAsync();
    }

    private record TenantResult(Guid TenantId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);
}
