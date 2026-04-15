using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class RoleAuthorizationTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);

    private async Task<(Guid tenantId, string memberToken, string adminToken)> SetupTenantWithUsersAsync(
        string tenantName, string slug, string memberEmail, string adminEmail)
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = tenantName, slug });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();
        var tenantId = tenant!.TenantId;

        var memberToken = await GetAuthTokenAsync(Client, tenantId, memberEmail);
        var adminToken = await CreateAdminAsync(tenantId, adminEmail);

        return (tenantId, memberToken, adminToken);
    }

    // --- Tenant endpoint role tests ---

    [Fact]
    public async Task Member_CannotUpdateTenantName_Returns403()
    {
        var (tenantId, memberToken, _) = await SetupTenantWithUsersAsync(
            "Role Test A", "role-a", "member-a@role.com", "admin-a@role.com");

        SetTenantContext(Client, memberToken, tenantId);

        var response = await Client.PutAsJsonAsync($"/api/tenants/{tenantId}", new { name = "Hacked Name" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanUpdateTenantName_Returns200()
    {
        var (tenantId, _, adminToken) = await SetupTenantWithUsersAsync(
            "Role Test B", "role-b", "member-b@role.com", "admin-b@role.com");

        SetTenantContext(Client, adminToken, tenantId);

        var response = await Client.PutAsJsonAsync($"/api/tenants/{tenantId}", new { name = "Updated Name" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Member_CannotDeactivateTenant_Returns403()
    {
        var (tenantId, memberToken, _) = await SetupTenantWithUsersAsync(
            "Role Test C", "role-c", "member-c@role.com", "admin-c@role.com");

        SetTenantContext(Client, memberToken, tenantId);

        var response = await Client.DeleteAsync($"/api/tenants/{tenantId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanDeactivateTenant_Returns204()
    {
        var (tenantId, _, adminToken) = await SetupTenantWithUsersAsync(
            "Role Test D", "role-d", "member-d@role.com", "admin-d@role.com");

        SetTenantContext(Client, adminToken, tenantId);

        var response = await Client.DeleteAsync($"/api/tenants/{tenantId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- User endpoint role tests ---

    [Fact]
    public async Task Member_CannotUpdateUserRole_Returns403()
    {
        var (tenantId, memberToken, _) = await SetupTenantWithUsersAsync(
            "Role Test E", "role-e", "member-e@role.com", "admin-e@role.com");

        SetTenantContext(Client, memberToken, tenantId);

        var response = await Client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}/role", new { role = "admin" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Member_CannotDeactivateUser_Returns403()
    {
        var (tenantId, memberToken, _) = await SetupTenantWithUsersAsync(
            "Role Test F", "role-f", "member-f@role.com", "admin-f@role.com");

        SetTenantContext(Client, memberToken, tenantId);

        var response = await Client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanDeactivateUser_Returns204()
    {
        var (tenantId, _, adminToken) = await SetupTenantWithUsersAsync(
            "Role Test G", "role-g", "member-g@role.com", "admin-g@role.com");

        // Get the member's id via users list
        SetTenantContext(Client, adminToken, tenantId);
        var users = await Client.GetFromJsonAsync<List<UserResult>>("/api/users");
        var member = users!.First(u => u.Email == "member-g@role.com");

        var response = await Client.DeleteAsync($"/api/users/{member.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Admin_CanUpdateUserRole_Returns200()
    {
        var (tenantId, _, adminToken) = await SetupTenantWithUsersAsync(
            "Role Test H", "role-h", "member-h@role.com", "admin-h@role.com");

        SetTenantContext(Client, adminToken, tenantId);
        var users = await Client.GetFromJsonAsync<List<UserResult>>("/api/users");
        var member = users!.First(u => u.Email == "member-h@role.com");

        var response = await Client.PutAsJsonAsync($"/api/users/{member.Id}/role", new { role = "admin" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record UserResult(Guid Id, string Email, string Role, bool IsActive);
}