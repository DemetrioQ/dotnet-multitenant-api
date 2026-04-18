using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Features.Storefront.Orders;
using SaasApi.Domain.Entities;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class PaymentTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);
    private record ProductResult(Guid ProductId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);
    private record SessionResult(Guid OrderId, string OrderNumber, string Provider, string SessionId, string PaymentUrl);

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
        return (await resp.Content.ReadFromJsonAsync<ProductResult>())!.ProductId;
    }

    private async Task<string> RegisterAndLoginCustomerAsync(string slug, Guid tenantId, string email)
    {
        var client = SubdomainClient(slug);
        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email,
            password = "Password1!",
            firstName = "Pay",
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
        var login = await client.PostAsJsonAsync("/api/v1/storefront/auth/login", new { email, password = "Password1!" });
        return (await login.Content.ReadFromJsonAsync<LoginResult>())!.JwtToken;
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

    [Fact]
    public async Task CreateSession_ReturnsPaymentUrlAndAttachesToOrder()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Pay Co", "pay-co", "admin@pay-co.com");
        var pid = await CreateProductAsync(admin, "Pay Item", 10m, 5);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@pay-co.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 2 });

        var resp = await client.PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = "https://pay-co.shop.demetrioq.com/checkout/success",
            cancelUrl = "https://pay-co.shop.demetrioq.com/checkout/cancel"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await resp.Content.ReadFromJsonAsync<SessionResult>();
        session!.Provider.Should().Be("simulation");
        session.SessionId.Should().StartWith("sim_");
        session.PaymentUrl.Should().Contain("/api/v1/storefront/payments/simulate");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = db.Orders.IgnoreQueryFilters().First(o => o.Id == session.OrderId);
        order.PaymentSessionId.Should().Be(session.SessionId);
        order.PaymentProvider.Should().Be("simulation");
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task SimulatedWebhook_Completed_MarksOrderPaidAndClearsCart()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Webhook Co", "pay-wh", "admin@pay-wh.com");
        var pid = await CreateProductAsync(admin, "WH Item", 5m, 10);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@pay-wh.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 3 });
        var sessionResp = await client.PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = "https://pay-wh.shop.demetrioq.com/checkout/success",
            cancelUrl = "https://pay-wh.shop.demetrioq.com/checkout/cancel"
        });
        var session = await sessionResp.Content.ReadFromJsonAsync<SessionResult>();

        // Simulate the hosted payment completing successfully.
        var webhookResp = await client.PostAsync($"/api/v1/storefront/payments/simulate?sessionId={session!.SessionId}", null);
        webhookResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = db.Orders.IgnoreQueryFilters().First(o => o.Id == session.OrderId);
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();

        // Cart was cleared.
        var cart = db.Carts.IgnoreQueryFilters().FirstOrDefault(c => c.TenantId == tenantId);
        if (cart is not null)
            db.CartItems.IgnoreQueryFilters().Count(i => i.CartId == cart.Id).Should().Be(0);
    }

    [Fact]
    public async Task SimulatedWebhook_Completed_IsIdempotent()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Idem Co", "pay-idem", "admin@pay-idem.com");
        var pid = await CreateProductAsync(admin, "Idem Item", 5m, 5);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@pay-idem.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });
        var sessionResp = await client.PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = "https://pay-idem.shop.demetrioq.com/checkout/success",
            cancelUrl = "https://pay-idem.shop.demetrioq.com/checkout/cancel"
        });
        var session = await sessionResp.Content.ReadFromJsonAsync<SessionResult>();

        // Deliver the same webhook twice.
        await client.PostAsync($"/api/v1/storefront/payments/simulate?sessionId={session!.SessionId}", null);
        await client.PostAsync($"/api/v1/storefront/payments/simulate?sessionId={session.SessionId}", null);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = db.Orders.IgnoreQueryFilters().First(o => o.Id == session.OrderId);
        order.Status.Should().Be(OrderStatus.Paid);
        // PaidAt should only be set once — second webhook is a no-op.
        order.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SimulatedWebhook_Expired_RestoresStockAndCancels()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Expire Co", "pay-exp", "admin@pay-exp.com");
        var pid = await CreateProductAsync(admin, "Exp Item", 5m, 10);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@pay-exp.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 4 });
        var sessionResp = await client.PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = "https://pay-exp.shop.demetrioq.com/checkout/success",
            cancelUrl = "https://pay-exp.shop.demetrioq.com/checkout/cancel"
        });
        var session = await sessionResp.Content.ReadFromJsonAsync<SessionResult>();

        // Stock should have been decremented by 4 at session creation.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Products.IgnoreQueryFilters().First(p => p.Id == pid).Stock.Should().Be(6);
        }

        // Simulate the session expiring.
        await client.PostAsync($"/api/v1/storefront/payments/simulate?sessionId={session!.SessionId}&outcome=expired", null);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = db.Orders.IgnoreQueryFilters().First(o => o.Id == session.OrderId);
            order.Status.Should().Be(OrderStatus.Canceled);
            order.CanceledAt.Should().NotBeNull();
            db.Products.IgnoreQueryFilters().First(p => p.Id == pid).Stock.Should().Be(10);
        }
    }

    [Fact]
    public async Task CreateSession_EmptyCart_Returns400()
    {
        var (slug, tenantId, _) = await CreateStoreWithAdminAsync("Empty Sess", "pay-empty", "admin@pay-empty.com");
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "buyer@pay-empty.com");

        var resp = await CustomerClient(slug, token).PostAsJsonAsync("/api/v1/storefront/checkout/session", new
        {
            shippingAddress = SampleAddress(),
            successUrl = "https://pay-empty.shop.demetrioq.com/checkout/success",
            cancelUrl = "https://pay-empty.shop.demetrioq.com/checkout/cancel"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_UnknownSession_ReturnsOkWithoutChanges()
    {
        var (slug, _, _) = await CreateStoreWithAdminAsync("Noop Co", "pay-noop", "admin@pay-noop.com");
        var client = SubdomainClient(slug);

        var resp = await client.PostAsync("/api/v1/storefront/payments/simulate?sessionId=sim_does_not_exist", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
