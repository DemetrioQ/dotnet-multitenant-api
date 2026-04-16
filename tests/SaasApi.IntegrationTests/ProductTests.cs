using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class ProductTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateProduct_Returns201()
    {
        // Arrange — create tenant, register + login, set auth header
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Test Co", slug = "testco-prod" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "testco-prod");

        SetTenantContext(Client, token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", new
        {
            name = "Widget",
            description = "A great widget",
            price = 9.99,
            stock = 100
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetProduct_NotFound_Returns404()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/tenants", new { name = "Test Co 2", slug = "testco-prod2" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "testco-prod2");

        SetTenantContext(Client, token);

        var response = await Client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record TenantResult(Guid TenantId);
}
