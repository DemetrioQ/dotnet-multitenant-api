using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class TenantIsolationTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);
    private record ProductResult(Guid ProductId);

    private async Task<(HttpClient client, Guid tenantId)> CreateTenantClientAsync(string name, string slug, string email)
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name, slug });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var client = Factory.CreateClient();
        var token = await GetAuthTokenAsync(client, tenant!.TenantId, slug, email);
        SetTenantContext(client, token);

        return (client, tenant.TenantId);
    }

    [Fact]
    public async Task TenantB_CannotRead_TenantA_Product()
    {
        var (clientA, _) = await CreateTenantClientAsync("Isolation Co A", "iso-a", "userA@iso.com");
        var (clientB, _) = await CreateTenantClientAsync("Isolation Co B", "iso-b", "userB@iso.com");

        // Tenant A creates a product
        var createResponse = await clientA.PostAsJsonAsync("/api/products", new
        {
            name = "Secret Widget",
            description = "Only for tenant A",
            price = 9.99,
            stock = 10
        });
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResult>();

        // Tenant B tries to read it
        var response = await clientB.GetAsync($"/api/products/{product!.ProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantB_CannotUpdate_TenantA_Product()
    {
        var (clientA, _) = await CreateTenantClientAsync("Isolation Co C", "iso-c", "userC@iso.com");
        var (clientB, _) = await CreateTenantClientAsync("Isolation Co D", "iso-d", "userD@iso.com");

        var createResponse = await clientA.PostAsJsonAsync("/api/products", new
        {
            name = "Secret Widget",
            description = "Only for tenant A",
            price = 9.99,
            stock = 10
        });
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResult>();

        var response = await clientB.PutAsJsonAsync($"/api/products/{product!.ProductId}", new
        {
            name = "Hacked",
            description = "Hacked",
            price = 1.00,
            stock = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantB_CannotDelete_TenantA_Product()
    {
        var (clientA, _) = await CreateTenantClientAsync("Isolation Co E", "iso-e", "userE@iso.com");
        var (clientB, _) = await CreateTenantClientAsync("Isolation Co F", "iso-f", "userF@iso.com");

        var createResponse = await clientA.PostAsJsonAsync("/api/products", new
        {
            name = "Secret Widget",
            description = "Only for tenant A",
            price = 9.99,
            stock = 10
        });
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResult>();

        var response = await clientB.DeleteAsync($"/api/products/{product!.ProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TenantB_CannotSee_TenantA_Users()
    {
        var (clientA, _) = await CreateTenantClientAsync("Isolation Co G", "iso-g", "userG@iso.com");
        var (clientB, _) = await CreateTenantClientAsync("Isolation Co H", "iso-h", "userH@iso.com");

        // Tenant B lists users — should not see Tenant A's user
        var response = await clientB.GetAsync("/api/users");
        var users = await response.Content.ReadFromJsonAsync<List<UserResult>>();

        users!.Should().NotContain(u => u.Email == "userG@iso.com");
    }


    [Fact]
    public async Task Member_CannotDeactivate_AnyUser()
    {
        var (clientB, _) = await CreateTenantClientAsync("Isolation Co J", "iso-j", "userJ@iso.com");

        // clientB is a member — [Authorize(Roles = "admin")] blocks before the handler runs
        var response = await clientB.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private record UserResult(Guid Id, string Email, string Role, bool IsActive);
}