using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class TenantTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateTenant_Returns201()
    {
        var response = await Client.PostAsJsonAsync("/api/tenants", new
        {
            name = "Acme Corp",
            slug = "acme"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTenant_DuplicateSlug_Returns409()
    {
        await Client.PostAsJsonAsync("/api/tenants", new { name = "Alpha", slug = "alpha" });

        var response = await Client.PostAsJsonAsync("/api/tenants", new { name = "Alpha 2", slug = "alpha" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetMyTenant_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/tenants/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyTenant_AuthenticatedUser_ReturnsOwnTenant()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "My Tenant Co", slug = "my-tenant-co" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "my-tenant-co", "user@my-tenant-co.com");
        SetTenantContext(Client, token);

        var response = await Client.GetAsync("/api/tenants/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TenantDetailResult>();
        body!.Id.Should().Be(tenant.TenantId);
    }

    [Fact]
    public async Task GetMyTenant_UserCannotAccessOtherTenant()
    {
        var tenantAResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Tenant A", slug = "tenant-a-me" });
        var tenantA = await tenantAResponse.Content.ReadFromJsonAsync<TenantResult>();

        var tenantBResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Tenant B", slug = "tenant-b-me" });
        var tenantB = await tenantBResponse.Content.ReadFromJsonAsync<TenantResult>();

        // User from tenant A calls /me — should get tenant A, not tenant B
        var tokenA = await GetAuthTokenAsync(Client, tenantA!.TenantId, "tenant-a-me", "userA@me-test.com");
        SetTenantContext(Client, tokenA);

        var response = await Client.GetAsync("/api/tenants/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TenantDetailResult>();
        body!.Id.Should().Be(tenantA.TenantId);
        body.Id.Should().NotBe(tenantB!.TenantId);
    }

    private record TenantResult(Guid TenantId);
    private record TenantDetailResult(Guid Id, string Name, string Slug, bool IsActive);
}
