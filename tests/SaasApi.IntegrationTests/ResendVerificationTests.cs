using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class ResendVerificationTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ResendVerification_UnverifiedUser_Returns200AndSendsNewToken()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Resend Co 1", slug = "resend-co-1" });
        var tenantId = await GetTenantIdAsync("resend-co-1");

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "resend1@resendco.com",
            password = "Password1!", firstName = "Test", lastName = "User"
        });

        // Wait to clear the 2-minute cooldown by backdating the token
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "resend1@resendco.com");
            var token = db.EmailVerificationTokens.IgnoreQueryFilters().First(t => t.UserId == user.Id);
            // Backdate the token so cooldown has passed
            db.Entry(token).Property("CreatedAt").CurrentValue = DateTime.UtcNow.AddMinutes(-3);
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync("/api/v1/auth/resend-verification", new
        {
            slug = "resend-co-1",
            email = "resend1@resendco.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Confirm a fresh token was created
        using var scope2 = Factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var user2 = db2.Users.IgnoreQueryFilters().First(u => u.Email == "resend1@resendco.com");
        var newToken = db2.EmailVerificationTokens.IgnoreQueryFilters().First(t => t.UserId == user2.Id);
        newToken.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResendVerification_WithinCooldown_Returns200ButNoNewToken()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Resend Co 2", slug = "resend-co-2" });
        var tenantId = await GetTenantIdAsync("resend-co-2");

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "resend2@resendco.com",
            password = "Password1!", firstName = "Test", lastName = "User"
        });

        string originalToken;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "resend2@resendco.com");
            originalToken = db.EmailVerificationTokens.IgnoreQueryFilters().First(t => t.UserId == user.Id).Token;
        }

        // Request within cooldown (token was just created by registration)
        var response = await Client.PostAsJsonAsync("/api/v1/auth/resend-verification", new
        {
            slug = "resend-co-2",
            email = "resend2@resendco.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Token should be unchanged
        using var scope2 = Factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var user2 = db2.Users.IgnoreQueryFilters().First(u => u.Email == "resend2@resendco.com");
        var currentToken = db2.EmailVerificationTokens.IgnoreQueryFilters().First(t => t.UserId == user2.Id).Token;
        currentToken.Should().Be(originalToken);
    }

    [Fact]
    public async Task ResendVerification_AlreadyVerifiedUser_Returns200Silently()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Resend Co 3", slug = "resend-co-3" });
        var tenantId = await GetTenantIdAsync("resend-co-3");

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "resend3@resendco.com",
            password = "Password1!", firstName = "Test", lastName = "User"
        });

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "resend3@resendco.com");
            user.VerifyEmail();
            await db.SaveChangesAsync();
        }

        var response = await Client.PostAsJsonAsync("/api/v1/auth/resend-verification", new
        {
            slug = "resend-co-3",
            email = "resend3@resendco.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_UnknownEmail_Returns200Silently()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Resend Co 4", slug = "resend-co-4" });

        var response = await Client.PostAsJsonAsync("/api/v1/auth/resend-verification", new
        {
            slug = "resend-co-4",
            email = "nobody@resendco.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_UnverifiedEmail_CanResendAtIsWithinCooldownWindow()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Resend Co 5", slug = "resend-co-5" });
        var tenantId = await GetTenantIdAsync("resend-co-5");

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "resend5@resendco.com",
            password = "Password1!", firstName = "Test", lastName = "User"
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug = "resend-co-5",
            email = "resend5@resendco.com",
            password = "Password1!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await loginResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var canResendAt = body.GetProperty("canResendAt").GetDateTime();

        // Token was just created — canResendAt should be ~2 minutes in the future
        canResendAt.Should().BeAfter(DateTime.UtcNow);
        canResendAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(3));
    }

    private async Task<Guid> GetTenantIdAsync(string slug)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.Tenants.IgnoreQueryFilters().First(t => t.Slug == slug).Id;
    }
}
