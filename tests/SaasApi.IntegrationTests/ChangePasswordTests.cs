using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class ChangePasswordTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private async Task<(Guid tenantId, string slug, string jwt)> CreateVerifiedUserAsync(string slug, string email)
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = slug, slug });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email,
            password = "Password1!", firstName = "Test", lastName = "User"
        });

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.IgnoreQueryFilters().First(u => u.Email == email);
            user.VerifyEmail();
            await db.SaveChangesAsync();
        }

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug,
            email,
            password = "Password1!"
        });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();

        return (tenant.TenantId, slug, login!.JwtToken);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrent_Returns204AndRevokesRefreshTokens()
    {
        var (_, _, jwt) = await CreateVerifiedUserAsync("change-pw-1", "cp1@test.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await Client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Password1!",
            newPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "cp1@test.com");
        var active = db.RefreshTokens.IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id && r.RevokedAt == null)
            .ToList();
        active.Should().BeEmpty("all refresh tokens must be revoked after password change");
    }

    [Fact]
    public async Task ChangePassword_CanLoginWithNewPassword()
    {
        var (_, slug, jwt) = await CreateVerifiedUserAsync("change-pw-2", "cp2@test.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        await Client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Password1!",
            newPassword = "NewPassword1!"
        });

        Client.DefaultRequestHeaders.Authorization = null;

        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug,
            email = "cp2@test.com",
            password = "NewPassword1!"
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns400()
    {
        var (_, _, jwt) = await CreateVerifiedUserAsync("change-pw-3", "cp3@test.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await Client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "WrongPassword!",
            newPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_SameAsCurrent_Returns400()
    {
        var (_, _, jwt) = await CreateVerifiedUserAsync("change-pw-4", "cp4@test.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await Client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Password1!",
            newPassword = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Password1!",
            newPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ShortNewPassword_Returns400()
    {
        var (_, _, jwt) = await CreateVerifiedUserAsync("change-pw-5", "cp5@test.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response = await Client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "Password1!",
            newPassword = "short"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record TenantResult(Guid TenantId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);
}
