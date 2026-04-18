using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.Storefront.Queries;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class StorefrontTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);
    private record ProductResult(Guid ProductId);

    private HttpClient CreatePublicClient(string host)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Host = host;
        return c;
    }

    private HttpClient CreateAdminClient(string token)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<(string slug, Guid tenantId, string adminToken)> CreateStorefrontAsync(
        string name, string slug, string email)
    {
        var tenantResp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name, slug });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<TenantResult>();
        var token = await CreateAdminAsync(tenant!.TenantId, slug, email);
        return (slug, tenant.TenantId, token);
    }

    [Fact]
    public async Task Storefront_UnknownSubdomain_ReturnsNotFound()
    {
        var client = CreatePublicClient("definitely-missing-xyz.shop.demetrioq.com");

        var response = await client.GetAsync("/api/v1/storefront/products");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Storefront_InactiveTenant_ReturnsNotFound()
    {
        var (slug, tenantId, _) = await CreateStorefrontAsync(
            "Inactive Store", "sf-inactive", "admin@sf-inactive.com");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = db.Tenants.IgnoreQueryFilters().First(x => x.Id == tenantId);
            t.Deactivate();
            await db.SaveChangesAsync();
        }

        var client = CreatePublicClient($"{slug}.shop.demetrioq.com");

        var response = await client.GetAsync("/api/v1/storefront/products");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Storefront_ValidSubdomain_ReturnsOnlyThatTenantsProducts()
    {
        var (slugA, _, tokenA) = await CreateStorefrontAsync(
            "Storefront A", "sf-a", "admin@sf-a.com");
        var adminA = CreateAdminClient(tokenA);
        await adminA.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Alpha Widget",
            description = "A widget",
            price = 10m,
            stock = 5
        });
        await adminA.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Beta Gadget",
            description = "A gadget",
            price = 20m,
            stock = 5
        });

        var (_, _, tokenB) = await CreateStorefrontAsync(
            "Storefront B", "sf-b", "admin@sf-b.com");
        var adminB = CreateAdminClient(tokenB);
        await adminB.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Gamma Thing",
            description = "Should not leak",
            price = 30m,
            stock = 5
        });

        var shopper = CreatePublicClient($"{slugA}.shop.demetrioq.com");
        var response = await shopper.GetAsync("/api/v1/storefront/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<StorefrontProductDto>>();
        paged!.Items.Select(p => p.Name).Should()
            .Contain(new[] { "Alpha Widget", "Beta Gadget" })
            .And.NotContain("Gamma Thing");
    }

    [Fact]
    public async Task Storefront_InactiveProduct_IsHidden()
    {
        var (slug, _, token) = await CreateStorefrontAsync(
            "Storefront Hidden", "sf-hidden", "admin@sf-hidden.com");
        var admin = CreateAdminClient(token);

        var createResp = await admin.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Hidden Product",
            description = "Deactivated",
            price = 15m,
            stock = 1
        });
        var product = await createResp.Content.ReadFromJsonAsync<ProductResult>();

        var statusResp = await admin.PutAsJsonAsync(
            $"/api/v1/products/{product!.ProductId}/status",
            new { isActive = false });
        statusResp.EnsureSuccessStatusCode();

        var shopper = CreatePublicClient($"{slug}.shop.demetrioq.com");
        var response = await shopper.GetAsync("/api/v1/storefront/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<StorefrontProductDto>>();
        paged!.Items.Should().NotContain(p => p.Name == "Hidden Product");
    }

    [Fact]
    public async Task Storefront_GetProductBySlug_ReturnsProduct()
    {
        var (slug, _, token) = await CreateStorefrontAsync(
            "Storefront Slug", "sf-slug", "admin@sf-slug.com");
        var admin = CreateAdminClient(token);

        await admin.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Great Mug",
            description = "Coffee mug",
            price = 12.5m,
            stock = 100,
            slug = "great-mug"
        });

        var shopper = CreatePublicClient($"{slug}.shop.demetrioq.com");
        var response = await shopper.GetAsync("/api/v1/storefront/products/great-mug");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<StorefrontProductDto>();
        product!.Name.Should().Be("Great Mug");
        product.Slug.Should().Be("great-mug");
    }

    [Fact]
    public async Task Storefront_GetProductBySlug_UnknownSlug_Returns404()
    {
        var (slug, _, _) = await CreateStorefrontAsync(
            "Storefront Missing Slug", "sf-missingslug", "admin@sf-missingslug.com");

        var shopper = CreatePublicClient($"{slug}.shop.demetrioq.com");
        var response = await shopper.GetAsync("/api/v1/storefront/products/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Storefront_GetStoreInfo_ReturnsTenantDetails()
    {
        var (slug, _, _) = await CreateStorefrontAsync(
            "Storefront Info Co", "sf-info", "admin@sf-info.com");

        var shopper = CreatePublicClient($"{slug}.shop.demetrioq.com");
        var response = await shopper.GetAsync("/api/v1/storefront/store");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var info = await response.Content.ReadFromJsonAsync<StorefrontInfoDto>();
        info!.Slug.Should().Be(slug);
        info.Name.Should().Be("Storefront Info Co");
        info.Currency.Should().NotBeNullOrWhiteSpace();
    }
}
