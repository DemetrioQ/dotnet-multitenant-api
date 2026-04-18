using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Features.Storefront.Cart;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class CartTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);
    private record ProductResult(Guid ProductId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);

    private HttpClient SubdomainClient(string slug)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Host = $"{slug}.shop.demetrioq.com";
        return c;
    }

    private async Task<(string slug, Guid tenantId, string merchantToken)> CreateStoreWithAdminAsync(
        string name, string slug, string adminEmail)
    {
        var tenantResp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name, slug });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<TenantResult>();
        var token = await CreateAdminAsync(tenant!.TenantId, slug, adminEmail);
        return (slug, tenant.TenantId, token);
    }

    private async Task<Guid> CreateProductAsync(string merchantToken, string name, decimal price, int stock)
    {
        var merchant = Factory.CreateClient();
        merchant.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", merchantToken);

        var resp = await merchant.PostAsJsonAsync("/api/v1/products", new
        {
            name,
            description = "Test product",
            price,
            stock
        });
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ProductResult>();
        return result!.ProductId;
    }

    private async Task<string> RegisterAndLoginCustomerAsync(string slug, Guid tenantId, string email)
    {
        var client = SubdomainClient(slug);
        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email,
            password = "Password1!",
            firstName = "Test",
            lastName = "Customer"
        });

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = db.Customers.IgnoreQueryFilters()
                .First(c => c.Email == email && c.TenantId == tenantId);
            customer.VerifyEmail();
            await db.SaveChangesAsync();
        }

        var loginResp = await client.PostAsJsonAsync("/api/v1/storefront/auth/login", new { email, password = "Password1!" });
        loginResp.EnsureSuccessStatusCode();
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResult>();
        return login!.JwtToken;
    }

    private HttpClient CustomerClient(string slug, string jwt)
    {
        var c = SubdomainClient(slug);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return c;
    }

    [Fact]
    public async Task GetCart_Empty_ReturnsEmptyCart()
    {
        var (slug, tenantId, _) = await CreateStoreWithAdminAsync("Empty Cart Co", "cart-empty", "admin@cart-empty.com");
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@cart-empty.com");

        var response = await CustomerClient(slug, token).GetAsync("/api/v1/storefront/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
        cart.Subtotal.Should().Be(0);
        cart.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task AddItem_CreatesCartAndReturnsLine()
    {
        var (slug, tenantId, merchantToken) = await CreateStoreWithAdminAsync("Add Co", "cart-add", "admin@cart-add.com");
        var productId = await CreateProductAsync(merchantToken, "Widget", 9.99m, 10);
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@cart-add.com");

        var response = await CustomerClient(slug, customerToken)
            .PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId, quantity = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].ProductId.Should().Be(productId);
        cart.Items[0].Quantity.Should().Be(2);
        cart.Subtotal.Should().Be(19.98m);
        cart.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task AddItem_SameProductTwice_IncrementsQuantity()
    {
        var (slug, tenantId, merchantToken) = await CreateStoreWithAdminAsync("Inc Co", "cart-inc", "admin@cart-inc.com");
        var productId = await CreateProductAsync(merchantToken, "Mug", 5m, 20);
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@cart-inc.com");
        var client = CustomerClient(slug, customerToken);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId, quantity = 1 });
        var response = await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId, quantity = 3 });

        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(4);
    }

    [Fact]
    public async Task AddItem_ExceedsStock_Returns400()
    {
        var (slug, tenantId, merchantToken) = await CreateStoreWithAdminAsync("Stock Co", "cart-stock", "admin@cart-stock.com");
        var productId = await CreateProductAsync(merchantToken, "Scarce", 3m, 2);
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@cart-stock.com");

        var response = await CustomerClient(slug, customerToken)
            .PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId, quantity = 5 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateItem_SetsQuantity()
    {
        var (slug, tenantId, merchantToken) = await CreateStoreWithAdminAsync("Update Co", "cart-upd", "admin@cart-upd.com");
        var productId = await CreateProductAsync(merchantToken, "Updated", 7m, 10);
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@cart-upd.com");
        var client = CustomerClient(slug, customerToken);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId, quantity = 2 });
        var response = await client.PutAsJsonAsync($"/api/v1/storefront/cart/items/{productId}", new { quantity = 5 });

        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task RemoveItem_EmptiesLine()
    {
        var (slug, tenantId, merchantToken) = await CreateStoreWithAdminAsync("Remove Co", "cart-rm", "admin@cart-rm.com");
        var productId = await CreateProductAsync(merchantToken, "Gone", 1m, 5);
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@cart-rm.com");
        var client = CustomerClient(slug, customerToken);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId, quantity = 1 });
        var response = await client.DeleteAsync($"/api/v1/storefront/cart/items/{productId}");

        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCart_RemovesAllLines()
    {
        var (slug, tenantId, merchantToken) = await CreateStoreWithAdminAsync("Clear Co", "cart-clr", "admin@cart-clr.com");
        var p1 = await CreateProductAsync(merchantToken, "One", 1m, 5);
        var p2 = await CreateProductAsync(merchantToken, "Two", 2m, 5);
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@cart-clr.com");
        var client = CustomerClient(slug, customerToken);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = p1, quantity = 1 });
        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = p2, quantity = 1 });

        var clearResp = await client.DeleteAsync("/api/v1/storefront/cart");
        clearResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cart = (await (await client.GetAsync("/api/v1/storefront/cart"))
            .Content.ReadFromJsonAsync<CartDto>())!;
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Cart_MerchantJwt_DeniedByCustomerOnlyPolicy()
    {
        var (slug, _, merchantToken) = await CreateStoreWithAdminAsync("Policy Co", "cart-policy", "admin@cart-policy.com");

        var client = SubdomainClient(slug);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", merchantToken);

        var response = await client.GetAsync("/api/v1/storefront/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cart_NoAuth_Returns401()
    {
        var client = SubdomainClient("cart-anon");

        var response = await client.GetAsync("/api/v1/storefront/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cart_CrossStoreProduct_ReturnsNotFound()
    {
        // Customer on store A tries to add a product that belongs to store B.
        var (slugA, tenantA, adminA) = await CreateStoreWithAdminAsync("Iso A", "cart-iso-a", "admin@cart-iso-a.com");
        var (_, _, adminB)           = await CreateStoreWithAdminAsync("Iso B", "cart-iso-b", "admin@cart-iso-b.com");
        var productB = await CreateProductAsync(adminB, "B Only", 1m, 5);

        var customerToken = await RegisterAndLoginCustomerAsync(slugA, tenantA, "buyer@cart-iso-a.com");

        var response = await CustomerClient(slugA, customerToken)
            .PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = productB, quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
