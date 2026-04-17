using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class RefreshTokenFamilyTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private async Task<(string slug, Guid tenantId)> CreateTenantAndUserAsync(string slug, string email)
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = slug, slug });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant!.TenantId,
            email,
            password = "Password1!", firstName = "Test", lastName = "User"
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == email);
        user.VerifyEmail();
        await db.SaveChangesAsync();

        return (slug, tenant.TenantId);
    }

    [Fact]
    public async Task RefreshToken_NewTokenCarriesSameFamilyId()
    {
        var (slug, _) = await CreateTenantAndUserAsync("family-tracking-1", "family1@test.com");

        await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug,
            email = "family1@test.com",
            password = "Password1!"
        });

        // Rotate once
        await Client.PostAsync("/api/v1/auth/refresh", null);

        // Both the original and new token should share the same FamilyId
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var tokens = db.RefreshTokens.IgnoreQueryFilters()
            .Where(r => db.Users.IgnoreQueryFilters().Any(u => u.Email == "family1@test.com" && u.Id == r.UserId))
            .ToList();

        tokens.Should().HaveCount(2);
        tokens.Select(t => t.FamilyId).Distinct().Should().HaveCount(1, "both tokens must share the same FamilyId");
    }

    [Fact]
    public async Task RefreshToken_ReusingRevokedToken_Returns401AndWipesFamily()
    {
        var (slug, _) = await CreateTenantAndUserAsync("family-tracking-2", "family2@test.com");

        // Capture the original refresh token cookie before rotation
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug,
            email = "family2@test.com",
            password = "Password1!"
        });
        var originalCookie = loginResponse.Headers
            .GetValues("Set-Cookie")
            .First(c => c.StartsWith("refreshToken="));
        var originalTokenValue = originalCookie.Split(';')[0].Substring("refreshToken=".Length);

        // Rotate once — original token is now revoked
        await Client.PostAsync("/api/v1/auth/refresh", null);

        // Replay the original (now-revoked) token by creating a fresh client with that cookie
        var replayClient = Factory.CreateClient();
        replayClient.DefaultRequestHeaders.Add("Cookie", $"refreshToken={originalTokenValue}");

        var replayResponse = await replayClient.PostAsync("/api/v1/auth/refresh", null);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // All tokens in the family must now be revoked (force-logout)
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "family2@test.com");
        var activeTokens = db.RefreshTokens.IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id && r.RevokedAt == null)
            .ToList();

        activeTokens.Should().BeEmpty("token reuse must revoke the entire family");
    }

    [Fact]
    public async Task RefreshToken_DifferentLogins_HaveDifferentFamilyIds()
    {
        var (slug, _) = await CreateTenantAndUserAsync("family-tracking-3", "family3@test.com");

        // Login twice — each session should start a new family
        await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug,
            email = "family3@test.com",
            password = "Password1!"
        });

        var client2 = Factory.CreateClient();
        await client2.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug,
            email = "family3@test.com",
            password = "Password1!"
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var user = db.Users.IgnoreQueryFilters().First(u => u.Email == "family3@test.com");
        var families = db.RefreshTokens.IgnoreQueryFilters()
            .Where(r => r.UserId == user.Id)
            .Select(r => r.FamilyId)
            .Distinct()
            .ToList();

        families.Should().HaveCount(2, "each login must start a new token family");
    }

    private record TenantResult(Guid TenantId);
}
