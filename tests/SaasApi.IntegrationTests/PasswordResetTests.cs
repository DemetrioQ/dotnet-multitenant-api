using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class PasswordResetTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ForgotPassword_WithValidEmail_Returns200()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Reset Co 1", slug = "reset-co-1" });

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = await GetTenantIdAsync("reset-co-1"),
            email = "forgot1@resetco.com",
            password = "Password1!", firstName = "Test", lastName = "User"
        });

        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            slug = "reset-co-1",
            email = "forgot1@resetco.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_StillReturns200()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Reset Co 2", slug = "reset-co-2" });

        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            slug = "reset-co-2",
            email = "nobody@resetco.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_AllowsLoginWithNewPassword()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Reset Co 3", slug = "reset-co-3" });
        var tenantId = await GetTenantIdAsync("reset-co-3");

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "reset3@resetco.com",
            password = "OldPass1!",
            firstName = "Test", lastName = "User"
        });

        // Trigger forgot password to create the token
        await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            slug = "reset-co-3",
            email = "reset3@resetco.com"
        });

        // Read the token directly from DB
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "reset3@resetco.com");
        var token = db.PasswordResetTokens.IgnoreQueryFilters().First(t => t.UserId == user.Id).Token;

        var resetResponse = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token,
            newPassword = "NewPass1!"
        });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify old password no longer works
        var oldLoginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug = "reset-co-3",
            email = "reset3@resetco.com",
            password = "OldPass1!"
        });
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify new password works (need to verify email first)
        user = db.Users.IgnoreQueryFilters().First(u => u.Email == "reset3@resetco.com");
        user.VerifyEmail();
        await db.SaveChangesAsync();

        var newLoginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug = "reset-co-3",
            email = "reset3@resetco.com",
            password = "NewPass1!"
        });
        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns404()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token = "not-a-real-token",
            newPassword = "NewPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetPassword_TokenIsDeletedAfterUse()
    {
        await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Reset Co 5", slug = "reset-co-5" });
        var tenantId = await GetTenantIdAsync("reset-co-5");

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "reset5@resetco.com",
            password = "OldPass1!",
            firstName = "Test", lastName = "User"
        });

        await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            slug = "reset-co-5",
            email = "reset5@resetco.com"
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "reset5@resetco.com");
        var token = db.PasswordResetTokens.IgnoreQueryFilters().First(t => t.UserId == user.Id).Token;

        // Use the token once
        await Client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, newPassword = "NewPass1!" });

        // Using it again should return 404
        var replayResponse = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token,
            newPassword = "AnotherPass1!"
        });
        replayResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> GetTenantIdAsync(string slug)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.Tenants.IgnoreQueryFilters().First(t => t.Slug == slug).Id;
    }
}
