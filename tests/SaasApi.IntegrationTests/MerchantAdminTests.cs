using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.MerchantCustomers;
using SaasApi.Application.Features.MerchantOrders;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class MerchantAdminTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);
    private record ProductResult(Guid ProductId);
    private record SessionResult(Guid OrderId, string OrderNumber, string Provider, string SessionId, string PaymentUrl);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);

    private HttpClient MerchantClient(string token)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private HttpClient SubdomainClient(string slug)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Host = $"{slug}.shop.demetrioq.com";
        return c;
    }

    private async Task<(string slug, Guid tenantId, string merchantToken)> CreateStoreWithAdminAsync(string name, string slug, string adminEmail)
    {
        var tenantResp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name, slug });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<TenantResult>();
        var token = await CreateAdminAsync(tenant!.TenantId, slug, adminEmail);
        return (slug, tenant.TenantId, token);
    }

    private async Task<Guid> CreateProductAsync(string merchantToken, string name, decimal price, int stock)
    {
        var resp = await MerchantClient(merchantToken).PostAsJsonAsync("/api/v1/products", new
        {
            name,
            description = "Test",
            price,
            stock
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ProductResult>())!.ProductId;
    }

    private async Task<string> RegisterAndLoginCustomerAsync(string slug, Guid tenantId, string email)
    {
        var client = SubdomainClient(slug);
        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email,
            password = "Password1!",
            firstName = "Mer",
            lastName = "Test"
        });
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c = db.Customers.IgnoreQueryFilters().First(x => x.Email == email && x.TenantId == tenantId);
            c.VerifyEmail();
            await db.SaveChangesAsync();
        }
        var loginResp = await client.PostAsJsonAsync("/api/v1/storefront/auth/login", new { email, password = "Password1!" });
        return (await loginResp.Content.ReadFromJsonAsync<LoginResult>())!.JwtToken;
    }

    private HttpClient CustomerClient(string slug, string jwt)
    {
        var c = SubdomainClient(slug);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return c;
    }

    private static object SampleAddress() => new
    {
        line1 = "1 Way",
        line2 = (string?)null,
        city = "X",
        region = "CA",
        postalCode = "00000",
        country = "US"
    };

    private async Task<Guid> PlacePaidOrderAsync(string slug, Guid tenantId, string merchantToken, string customerEmail, Guid productId, int quantity)
    {
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, customerEmail);
        var client = CustomerClient(slug, customerToken);
        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId, quantity });
        var sessResp = await client.PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = $"https://{slug}.shop.demetrioq.com/checkout/success",
            cancelUrl = $"https://{slug}.shop.demetrioq.com/checkout/cancel"
        });
        var session = await sessResp.Content.ReadFromJsonAsync<SessionResult>();
        await client.PostAsync($"/api/v1/storefront/payments/simulate?sessionId={session!.SessionId}", null);
        return session.OrderId;
    }

    [Fact]
    public async Task GetOrders_ListsAllOrdersInTenant()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Merchant A", "adm-orders", "admin@adm-orders.com");
        var pid = await CreateProductAsync(admin, "List Item", 10m, 50);

        await PlacePaidOrderAsync(slug, tenantId, admin, "c1@adm-orders.com", pid, 2);
        await PlacePaidOrderAsync(slug, tenantId, admin, "c2@adm-orders.com", pid, 1);

        var resp = await MerchantClient(admin).GetAsync("/api/v1/orders");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await resp.Content.ReadFromJsonAsync<PagedResult<MerchantOrderSummaryDto>>();
        page!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        page.Items.Should().OnlyContain(o => o.Status == "paid");
    }

    [Fact]
    public async Task GetOrders_StatusFilter_Works()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Filter Co", "adm-filter", "admin@adm-filter.com");
        var pid = await CreateProductAsync(admin, "Filter Item", 5m, 50);

        // One paid order.
        await PlacePaidOrderAsync(slug, tenantId, admin, "c1@adm-filter.com", pid, 1);

        // One pending order (session created but no webhook simulation).
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "c2@adm-filter.com");
        var client = CustomerClient(slug, customerToken);
        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });
        await client.PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = "https://adm-filter.shop.demetrioq.com/s",
            cancelUrl = "https://adm-filter.shop.demetrioq.com/c"
        });

        var resp = await MerchantClient(admin).GetAsync("/api/v1/orders?status=pending");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await resp.Content.ReadFromJsonAsync<PagedResult<MerchantOrderSummaryDto>>();
        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(o => o.Status == "pending");
    }

    [Fact]
    public async Task FulfillOrder_OnPaidOrder_MarksFulfilled()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Fulfill Co", "adm-fulfill", "admin@adm-fulfill.com");
        var pid = await CreateProductAsync(admin, "F Item", 7m, 20);
        var orderId = await PlacePaidOrderAsync(slug, tenantId, admin, "c@adm-fulfill.com", pid, 1);

        var resp = await MerchantClient(admin).PostAsync($"/api/v1/orders/{orderId}/fulfill", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await resp.Content.ReadFromJsonAsync<MerchantOrderDto>();
        order!.Status.Should().Be("fulfilled");
        order.FulfilledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FulfillOrder_OnPendingOrder_Returns400()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Early Co", "adm-early", "admin@adm-early.com");
        var pid = await CreateProductAsync(admin, "E Item", 1m, 10);

        // Place an order but don't complete payment — it stays pending.
        var customerToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "c@adm-early.com");
        var client = CustomerClient(slug, customerToken);
        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });
        var sess = await client.PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = "https://adm-early.shop.demetrioq.com/s",
            cancelUrl = "https://adm-early.shop.demetrioq.com/c"
        });
        var session = await sess.Content.ReadFromJsonAsync<SessionResult>();

        var resp = await MerchantClient(admin).PostAsync($"/api/v1/orders/{session!.OrderId}/fulfill", null);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOrder_RestoresStock()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Cancel Co", "adm-cancel", "admin@adm-cancel.com");
        var pid = await CreateProductAsync(admin, "C Item", 3m, 10);
        var orderId = await PlacePaidOrderAsync(slug, tenantId, admin, "c@adm-cancel.com", pid, 3);

        // Stock should be 7 now (10 - 3).
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Products.IgnoreQueryFilters().First(p => p.Id == pid).Stock.Should().Be(7);
        }

        var resp = await MerchantClient(admin).PostAsync($"/api/v1/orders/{orderId}/cancel", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = db.Orders.IgnoreQueryFilters().First(o => o.Id == orderId);
            order.Status.Should().Be(SaasApi.Domain.Entities.OrderStatus.Canceled);
            db.Products.IgnoreQueryFilters().First(p => p.Id == pid).Stock.Should().Be(10);
        }
    }

    [Fact]
    public async Task GetCustomers_ReturnsCustomersWithAggregates()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("CustList Co", "adm-custlist", "admin@adm-custlist.com");
        var pid = await CreateProductAsync(admin, "CL Item", 20m, 50);
        await PlacePaidOrderAsync(slug, tenantId, admin, "alice@adm-custlist.com", pid, 2);

        var resp = await MerchantClient(admin).GetAsync("/api/v1/customers");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await resp.Content.ReadFromJsonAsync<PagedResult<MerchantCustomerSummaryDto>>();
        var alice = page!.Items.FirstOrDefault(c => c.Email == "alice@adm-custlist.com");
        alice.Should().NotBeNull();
        alice!.OrderCount.Should().Be(1);
        alice.LifetimeSpend.Should().Be(40m);
    }

    [Fact]
    public async Task GetCustomerById_ReturnsDetailWithOrders()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("CustDet Co", "adm-custdet", "admin@adm-custdet.com");
        var pid = await CreateProductAsync(admin, "CD Item", 9m, 30);
        await PlacePaidOrderAsync(slug, tenantId, admin, "bob@adm-custdet.com", pid, 1);
        await PlacePaidOrderAsync(slug, tenantId, admin, "bob@adm-custdet.com", pid, 1);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bob = db.Customers.IgnoreQueryFilters().First(c => c.Email == "bob@adm-custdet.com");

        var resp = await MerchantClient(admin).GetAsync($"/api/v1/customers/{bob.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await resp.Content.ReadFromJsonAsync<MerchantCustomerDto>();
        detail!.Email.Should().Be("bob@adm-custdet.com");
        detail.OrderCount.Should().Be(2);
        detail.LifetimeSpend.Should().Be(18m);
        detail.Orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task Dashboard_ShowsRevenueAndCustomerCount()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Dash Co", "adm-dash", "admin@adm-dash.com");
        var pid = await CreateProductAsync(admin, "Dash Item", 50m, 20);
        await PlacePaidOrderAsync(slug, tenantId, admin, "d1@adm-dash.com", pid, 1);
        await PlacePaidOrderAsync(slug, tenantId, admin, "d2@adm-dash.com", pid, 2);

        var resp = await MerchantClient(admin).GetAsync("/api/v1/tenants/dashboard");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<DashboardSnapshot>();
        body!.CustomerCount.Should().BeGreaterThanOrEqualTo(2);
        body.PaidOrderCount.Should().BeGreaterThanOrEqualTo(2);
        body.GrossRevenue.Should().BeGreaterThanOrEqualTo(150m); // 50*1 + 50*2
        body.PlatformFees.Should().BeGreaterThan(0m);
        body.NetRevenue.Should().Be(body.GrossRevenue - body.PlatformFees);
        body.CurrentFeePercent.Should().Be(0.05m);
        body.TopProducts.Should().Contain(tp => tp.Name == "Dash Item");
    }

    private record DashboardTopProduct(Guid ProductId, string Name, string Slug, int UnitsSold, decimal Revenue);
    private record DashboardSnapshot(
        int UserCount,
        int ActiveUserCount,
        int ProductCount,
        int ActiveProductCount,
        int CustomerCount,
        int PendingOrderCount,
        int PaidOrderCount,
        decimal GrossRevenue,
        decimal PlatformFees,
        decimal NetRevenue,
        decimal CurrentFeePercent,
        decimal AverageOrderValue,
        IReadOnlyList<DashboardTopProduct> TopProducts,
        bool OnboardingComplete);
}
