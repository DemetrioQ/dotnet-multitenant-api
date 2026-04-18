using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.Storefront.Cart;
using SaasApi.Application.Features.Storefront.Orders;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class CheckoutTests(WebAppFactory factory) : IntegrationTestBase(factory)
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
            description = "Test",
            price,
            stock
        });
        resp.EnsureSuccessStatusCode();
        var p = await resp.Content.ReadFromJsonAsync<ProductResult>();
        return p!.ProductId;
    }

    private async Task<string> RegisterAndLoginCustomerAsync(string slug, Guid tenantId, string email)
    {
        var client = SubdomainClient(slug);
        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email,
            password = "Password1!",
            firstName = "Buy",
            lastName = "Er"
        });

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c = db.Customers.IgnoreQueryFilters()
                .First(x => x.Email == email && x.TenantId == tenantId);
            c.VerifyEmail();
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

    private static object SampleAddress() => new
    {
        line1 = "123 Main St",
        line2 = (string?)null,
        city = "Portland",
        region = "OR",
        postalCode = "97201",
        country = "US"
    };

    [Fact]
    public async Task Checkout_EmptyCart_Returns400()
    {
        var (slug, tenantId, _) = await CreateStoreWithAdminAsync("Empty Chk", "chk-empty", "admin@chk-empty.com");
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@chk-empty.com");

        var response = await CustomerClient(slug, token).PostAsJsonAsync("/api/v1/storefront/checkout", new
        {
            shippingAddress = SampleAddress()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_WithItems_CreatesOrderAndClearsCart()
    {
        var (slug, tenantId, adminToken) = await CreateStoreWithAdminAsync("Flow Co", "chk-flow", "admin@chk-flow.com");
        var p1 = await CreateProductAsync(adminToken, "Widget", 10m, 10);
        var p2 = await CreateProductAsync(adminToken, "Gadget", 5m, 10);

        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@chk-flow.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = p1, quantity = 2 });
        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = p2, quantity = 3 });

        var checkoutResp = await client.PostAsJsonAsync("/api/v1/storefront/checkout", new
        {
            shippingAddress = SampleAddress()
        });

        checkoutResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();
        order!.Status.Should().Be("pending");
        order.Number.Should().StartWith("ORD-");
        order.Subtotal.Should().Be(35m); // 2*10 + 3*5
        order.Items.Should().HaveCount(2);

        // Cart is empty after checkout.
        var cartResp = await client.GetAsync("/api/v1/storefront/cart");
        var cart = await cartResp.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Checkout_DecrementsStock()
    {
        var (slug, tenantId, adminToken) = await CreateStoreWithAdminAsync("Stock Chk", "chk-stock", "admin@chk-stock.com");
        var pid = await CreateProductAsync(adminToken, "Stocky", 2m, 10);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@chk-stock.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 4 });
        var resp = await client.PostAsJsonAsync("/api/v1/storefront/checkout", new { shippingAddress = SampleAddress() });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = db.Products.IgnoreQueryFilters().First(p => p.Id == pid);
        product.Stock.Should().Be(6);
    }

    [Fact]
    public async Task Checkout_InsufficientStock_Returns400AndDoesNotDecrement()
    {
        var (slug, tenantId, adminToken) = await CreateStoreWithAdminAsync("Scarce Chk", "chk-scarce", "admin@chk-scarce.com");
        var pid = await CreateProductAsync(adminToken, "Scarce", 5m, 3);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@chk-scarce.com");
        var client = CustomerClient(slug, token);

        // Add within cart limit.
        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 3 });

        // Merchant reduces stock below cart quantity.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = db.Products.IgnoreQueryFilters().First(p => p.Id == pid);
            typeof(SaasApi.Domain.Entities.Product).GetMethod("Update")!
                .Invoke(product, new object[] { product.Name, product.Description, product.Price, 1 });
            await db.SaveChangesAsync();
        }

        var resp = await client.PostAsJsonAsync("/api/v1/storefront/checkout", new { shippingAddress = SampleAddress() });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Stock should still be 1 — no partial decrement.
        using var scope2 = Factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var p = db2.Products.IgnoreQueryFilters().First(x => x.Id == pid);
        p.Stock.Should().Be(1);
    }

    [Fact]
    public async Task GetMyOrders_ReturnsOnlyMyOrders()
    {
        var (slug, tenantId, adminToken) = await CreateStoreWithAdminAsync("Hist Co", "chk-hist", "admin@chk-hist.com");
        var pid = await CreateProductAsync(adminToken, "Hist Item", 3m, 100);

        var aliceToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "alice@chk-hist.com");
        var aliceClient = CustomerClient(slug, aliceToken);
        await aliceClient.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });
        await aliceClient.PostAsJsonAsync("/api/v1/storefront/checkout", new { shippingAddress = SampleAddress() });

        var bobToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "bob@chk-hist.com");
        var bobClient = CustomerClient(slug, bobToken);
        await bobClient.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 2 });
        await bobClient.PostAsJsonAsync("/api/v1/storefront/checkout", new { shippingAddress = SampleAddress() });

        var aliceOrdersResp = await aliceClient.GetAsync("/api/v1/storefront/orders");
        var aliceOrders = await aliceOrdersResp.Content.ReadFromJsonAsync<PagedResult<OrderSummaryDto>>();
        aliceOrders!.Items.Should().HaveCount(1);
        aliceOrders.Items[0].ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrderById_OtherCustomersOrder_Returns404()
    {
        var (slug, tenantId, adminToken) = await CreateStoreWithAdminAsync("ACL Co", "chk-acl", "admin@chk-acl.com");
        var pid = await CreateProductAsync(adminToken, "ACL", 1m, 10);

        var aliceToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "alice@chk-acl.com");
        var aliceClient = CustomerClient(slug, aliceToken);
        await aliceClient.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });
        var aliceCheckoutResp = await aliceClient.PostAsJsonAsync("/api/v1/storefront/checkout", new { shippingAddress = SampleAddress() });
        var aliceOrder = await aliceCheckoutResp.Content.ReadFromJsonAsync<OrderDto>();

        var bobToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "bob@chk-acl.com");
        var bobClient = CustomerClient(slug, bobToken);

        var resp = await bobClient.GetAsync($"/api/v1/storefront/orders/{aliceOrder!.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Checkout_SnapshotsProductData()
    {
        var (slug, tenantId, adminToken) = await CreateStoreWithAdminAsync("Snap Co", "chk-snap", "admin@chk-snap.com");
        var pid = await CreateProductAsync(adminToken, "Original Name", 10m, 5);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@chk-snap.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });
        var checkoutResp = await client.PostAsJsonAsync("/api/v1/storefront/checkout", new { shippingAddress = SampleAddress() });
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();

        // Merchant renames product and changes price.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var p = db.Products.IgnoreQueryFilters().First(x => x.Id == pid);
            typeof(SaasApi.Domain.Entities.Product).GetMethod("Update")!
                .Invoke(p, new object[] { "Renamed", p.Description, 99m, p.Stock });
            await db.SaveChangesAsync();
        }

        // Fetch order — OrderItem should still show the original snapshot.
        var detailResp = await client.GetAsync($"/api/v1/storefront/orders/{order!.Id}");
        var detail = await detailResp.Content.ReadFromJsonAsync<OrderDto>();
        detail!.Items[0].ProductName.Should().Be("Original Name");
        detail.Items[0].UnitPrice.Should().Be(10m);
    }

}
