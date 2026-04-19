using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class InvitationTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Invite_AsAdmin_Returns200AndCreatesInvitation()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Invite Co 1", slug = "invite-co-1" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await CreateAdminAsync(tenant!.TenantId, "invite-co-1", "admin1@invite.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await Client.PostAsJsonAsync("/api/v1/invitations", new { email = "newuser1@invite.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var invitation = db.Invitations.IgnoreQueryFilters()
            .FirstOrDefault(i => i.Email == "newuser1@invite.com" && i.TenantId == tenant.TenantId);
        invitation.Should().NotBeNull();
        invitation!.AcceptedAt.Should().BeNull();
    }

    [Fact]
    public async Task Invite_AsMember_Returns403()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Invite Co 2", slug = "invite-co-2" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        // First user is admin; register a second user as member
        var adminToken = await CreateAdminAsync(tenant!.TenantId, "invite-co-2", "admin2@invite.com");
        await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant.TenantId,
            email = "member2@invite.com",
            password = "Password1!", firstName = "Test", lastName = "User"
        });
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
            var u = db.Users.IgnoreQueryFilters().First(u => u.Email == "member2@invite.com");
            u.VerifyEmail();
            await db.SaveChangesAsync();
        }
        var memberLogin = await Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug = "invite-co-2",
            email = "member2@invite.com",
            password = "Password1!"
        });
        var memberResult = await memberLogin.Content.ReadFromJsonAsync<LoginResult>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberResult!.JwtToken);

        var response = await Client.PostAsJsonAsync("/api/v1/invitations", new { email = "another@invite.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invite_ExistingUser_Returns200Silently()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Invite Co 3", slug = "invite-co-3" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await CreateAdminAsync(tenant!.TenantId, "invite-co-3", "admin3@invite.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Invite an email that is already a registered user
        var response = await Client.PostAsJsonAsync("/api/v1/invitations", new { email = "admin3@invite.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // No invitation should have been created
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var invitation = db.Invitations.IgnoreQueryFilters()
            .FirstOrDefault(i => i.Email == "admin3@invite.com" && i.TenantId == tenant.TenantId);
        invitation.Should().BeNull();
    }

    [Fact]
    public async Task AcceptInvitation_ValidToken_CreatesUserAndMarksAccepted()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Invite Co 4", slug = "invite-co-4" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await CreateAdminAsync(tenant!.TenantId, "invite-co-4", "admin4@invite.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await Client.PostAsJsonAsync("/api/v1/invitations", new { email = "invited4@invite.com" });

        // Get token from DB
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var invitation = db.Invitations.IgnoreQueryFilters()
            .First(i => i.Email == "invited4@invite.com");

        // Accept with a new client (no auth)
        var anonClient = Factory.CreateClient();
        var response = await anonClient.PostAsJsonAsync("/api/v1/invitations/accept", new
        {
            token = invitation.Token,
            password = "Password1!", firstName = "Invited", lastName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        db.ChangeTracker.Clear();
        var updated = db.Invitations.IgnoreQueryFilters().First(i => i.Id == invitation.Id);
        updated.AcceptedAt.Should().NotBeNull();

        var user = db.Users.IgnoreQueryFilters()
            .FirstOrDefault(u => u.Email == "invited4@invite.com" && u.TenantId == tenant.TenantId);
        user.Should().NotBeNull();
        user!.IsEmailVerified.Should().BeTrue();
        user.Role.Should().Be(SaasApi.Domain.Entities.UserRole.Member);
    }

    [Fact]
    public async Task AcceptInvitation_AllowsLoginImmediately()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Invite Co 5", slug = "invite-co-5" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await CreateAdminAsync(tenant!.TenantId, "invite-co-5", "admin5@invite.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await Client.PostAsJsonAsync("/api/v1/invitations", new { email = "invited5@invite.com" });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var token = db.Invitations.IgnoreQueryFilters()
            .First(i => i.Email == "invited5@invite.com").Token;

        var anonClient = Factory.CreateClient();
        await anonClient.PostAsJsonAsync("/api/v1/invitations/accept", new
        {
            token,
            password = "Password1!", firstName = "Invited", lastName = "User"
        });

        var loginResponse = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            slug = "invite-co-5",
            email = "invited5@invite.com",
            password = "Password1!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcceptInvitation_InvalidToken_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/invitations/accept", new
        {
            token = "invalid-token-xyz",
            password = "Password1!", firstName = "Invited", lastName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReInvite_ReplacesPendingInvitation()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Invite Co 6", slug = "invite-co-6" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await CreateAdminAsync(tenant!.TenantId, "invite-co-6", "admin6@invite.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await Client.PostAsJsonAsync("/api/v1/invitations", new { email = "reinvited@invite.com" });
        await Client.PostAsJsonAsync("/api/v1/invitations", new { email = "reinvited@invite.com" });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaasApi.Infrastructure.Persistence.AppDbContext>();
        var invitations = db.Invitations.IgnoreQueryFilters()
            .Where(i => i.Email == "reinvited@invite.com" && i.TenantId == tenant.TenantId)
            .ToList();

        invitations.Should().HaveCount(1, "re-inviting should replace the pending invitation");
    }

    private record TenantResult(Guid TenantId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);
}
