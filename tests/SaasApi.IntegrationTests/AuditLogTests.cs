using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class AuditLogTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAuditLog_AfterProductCreate_ContainsEntry()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Audit Co 1", slug = "audit-co-1" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await GetAuthTokenAsync(Client, tenant!.TenantId, "audit-co-1", "admin1@audit.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Audit Product",
            description = "Desc",
            price = 9.99,
            stock = 5
        });

        var response = await Client.GetAsync("/api/v1/audit?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuditLogResult>();
        body!.TotalCount.Should().BeGreaterThan(0);
        body.Items.Should().Contain(e => e.Action == "product.created");
    }

    [Fact]
    public async Task GetAuditLog_AfterProductUpdate_ContainsEntry()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Audit Co 2", slug = "audit-co-2" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await GetAuthTokenAsync(Client, tenant!.TenantId, "audit-co-2", "admin2@audit.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Audit Product 2",
            description = "Desc",
            price = 5.00,
            stock = 1
        });
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResult>();

        await Client.PutAsJsonAsync($"/api/v1/products/{product!.ProductId}", new
        {
            name = "Audit Product 2 Updated",
            description = "Updated desc",
            price = 6.00,
            stock = 2
        });

        var response = await Client.GetAsync("/api/v1/audit?page=1&pageSize=10");
        var body = await response.Content.ReadFromJsonAsync<AuditLogResult>();
        body!.Items.Should().Contain(e => e.Action == "product.updated");
    }

    [Fact]
    public async Task GetAuditLog_AfterTenantUpdate_ContainsEntry()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Audit Co 3", slug = "audit-co-3" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await GetAuthTokenAsync(Client, tenant!.TenantId, "audit-co-3", "admin3@audit.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await Client.PutAsJsonAsync($"/api/v1/tenants/{tenant.TenantId}", new
        {
            name = "Audit Co 3 Updated",
            timezone = "UTC",
            currency = "USD",
            supportEmail = (string?)null,
            websiteUrl = (string?)null
        });

        var response = await Client.GetAsync("/api/v1/audit?page=1&pageSize=10");
        var body = await response.Content.ReadFromJsonAsync<AuditLogResult>();
        body!.Items.Should().Contain(e => e.Action == "tenant.updated");
    }

    [Fact]
    public async Task GetAuditLog_Paginated_RespectsPageSize()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Audit Co 4", slug = "audit-co-4" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await GetAuthTokenAsync(Client, tenant!.TenantId, "audit-co-4", "admin4@audit.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Create 3 products to generate 3 audit entries
        for (var i = 1; i <= 3; i++)
            await Client.PostAsJsonAsync("/api/v1/products", new
            {
                name = $"Paginated Product {i}",
                description = "Desc",
                price = 1.00,
                stock = 1
            });

        var response = await Client.GetAsync("/api/v1/audit?page=1&pageSize=2");
        var body = await response.Content.ReadFromJsonAsync<AuditLogResult>();
        body!.Items.Count.Should().Be(2);
        body.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetAuditLog_AsMember_Returns403()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Audit Co 5", slug = "audit-co-5" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        // First user = admin, second = member
        await GetAuthTokenAsync(Client, tenant!.TenantId, "audit-co-5", "admin5@audit.com");
        var memberToken = await GetAuthTokenAsync(Client, tenant.TenantId, "audit-co-5", "member5@audit.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await Client.GetAsync("/api/v1/audit?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLog_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/v1/audit?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TenantResult(Guid TenantId);
    private record ProductResult(Guid ProductId);
    private record AuditLogEntryResult(Guid Id, Guid UserId, string UserEmail, string Action, string EntityType, Guid? EntityId, string? Details, DateTime CreatedAt);
    private record AuditLogResult(IReadOnlyList<AuditLogEntryResult> Items, int TotalCount, int Page, int PageSize);
}
