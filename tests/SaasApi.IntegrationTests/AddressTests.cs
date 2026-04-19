using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Features.Storefront.Addresses;
using SaasApi.Application.Features.Storefront.Orders;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class AddressTests(WebAppFactory factory) : IntegrationTestBase(factory)
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

    private async Task<(string slug, Guid tenantId, string merchantToken)> CreateStoreWithAdminAsync(string name, string slug, string adminEmail)
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
        var resp = await merchant.PostAsJsonAsync("/api/v1/products", new { name, description = "Test", price, stock });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ProductResult>())!.ProductId;
    }

    private async Task<string> RegisterAndLoginCustomerAsync(string slug, Guid tenantId, string email)
    {
        var client = SubdomainClient(slug);
        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email, password = "Password1!", firstName = "Addr", lastName = "Test"
        });
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var c = db.Customers.IgnoreQueryFilters().First(x => x.Email == email && x.TenantId == tenantId);
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

    private static object AddressBody(string label = "Home", string line1 = "123 Main", string city = "Portland",
        string region = "OR", string postal = "97201", string country = "US") => new
        {
            line1, line2 = (string?)null, city, region, postalCode = postal, country
        };

    [Fact]
    public async Task CreateAddress_FirstAddress_IsDefaultForBoth()
    {
        var (slug, tenantId, _) = await CreateStoreWithAdminAsync("Addr Co", "addr-1st", "admin@addr-1st.com");
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "c@addr-1st.com");
        var client = CustomerClient(slug, token);

        var resp = await client.PostAsJsonAsync("/api/v1/storefront/addresses", new
        {
            label = "Home",
            address = AddressBody()
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var a = await resp.Content.ReadFromJsonAsync<CustomerAddressDto>();
        a!.IsDefaultShipping.Should().BeTrue();
        a.IsDefaultBilling.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAddress_FlipsDefault_AndUnsetsOthers()
    {
        var (slug, tenantId, _) = await CreateStoreWithAdminAsync("Addr Co2", "addr-flip", "admin@addr-flip.com");
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "c@addr-flip.com");
        var client = CustomerClient(slug, token);

        var firstResp = await client.PostAsJsonAsync("/api/v1/storefront/addresses", new { label = "Home", address = AddressBody() });
        var first = await firstResp.Content.ReadFromJsonAsync<CustomerAddressDto>();

        var secondResp = await client.PostAsJsonAsync("/api/v1/storefront/addresses", new { label = "Work", address = AddressBody(line1: "999 Office Rd") });
        var second = await secondResp.Content.ReadFromJsonAsync<CustomerAddressDto>();

        // Flip shipping default to the second address.
        var update = await client.PutAsJsonAsync($"/api/v1/storefront/addresses/{second!.Id}", new
        {
            label = "Work",
            address = AddressBody(line1: "999 Office Rd"),
            isDefaultShipping = true,
            isDefaultBilling = false
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<CustomerAddressDto>();
        updated!.IsDefaultShipping.Should().BeTrue();

        // Fetch list — old default-shipping should be false now.
        var listResp = await client.GetAsync("/api/v1/storefront/addresses");
        var list = await listResp.Content.ReadFromJsonAsync<List<CustomerAddressDto>>();
        var refreshedFirst = list!.First(a => a.Id == first!.Id);
        refreshedFirst.IsDefaultShipping.Should().BeFalse();
        // But billing on the first should stay default since we only changed shipping.
        refreshedFirst.IsDefaultBilling.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAddress_Removes_AndIsGoneFromList()
    {
        var (slug, tenantId, _) = await CreateStoreWithAdminAsync("Addr Co3", "addr-del", "admin@addr-del.com");
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "c@addr-del.com");
        var client = CustomerClient(slug, token);

        var createResp = await client.PostAsJsonAsync("/api/v1/storefront/addresses", new { label = "Temp", address = AddressBody() });
        var a = await createResp.Content.ReadFromJsonAsync<CustomerAddressDto>();

        var del = await client.DeleteAsync($"/api/v1/storefront/addresses/{a!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await (await client.GetAsync("/api/v1/storefront/addresses")).Content.ReadFromJsonAsync<List<CustomerAddressDto>>();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task Addresses_RequireCustomerAuth()
    {
        var client = SubdomainClient("addr-anon");
        var response = await client.GetAsync("/api/v1/storefront/addresses");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_WithSavedAddressId_UsesSavedAddress()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Addr Chk", "addr-chk", "admin@addr-chk.com");
        var pid = await CreateProductAsync(admin, "Chk Item", 10m, 50);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "c@addr-chk.com");
        var client = CustomerClient(slug, token);

        var createAddr = await client.PostAsJsonAsync("/api/v1/storefront/addresses", new
        {
            label = "Home",
            address = new
            {
                line1 = "42 Saved St",
                line2 = (string?)null,
                city = "Seattle",
                region = "WA",
                postalCode = "98101",
                country = "US"
            }
        });
        var saved = await createAddr.Content.ReadFromJsonAsync<CustomerAddressDto>();

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });

        var checkoutResp = await client.PostAsJsonAsync("/api/v1/storefront/checkout", new
        {
            shippingAddressId = saved!.Id
            // billingAddress and billingAddressId both null → billing defaults to shipping
        });
        checkoutResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await checkoutResp.Content.ReadFromJsonAsync<OrderDto>();
        order!.ShippingAddress.Line1.Should().Be("42 Saved St");
        order.ShippingAddress.City.Should().Be("Seattle");
        order.BillingAddress.Line1.Should().Be("42 Saved St");
    }

    [Fact]
    public async Task Checkout_WithBothShippingInputs_Returns400()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Addr Both", "addr-both", "admin@addr-both.com");
        var pid = await CreateProductAsync(admin, "Both Item", 1m, 10);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "c@addr-both.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });

        var resp = await client.PostAsJsonAsync("/api/v1/storefront/checkout", new
        {
            shippingAddress = AddressBody(),
            shippingAddressId = Guid.NewGuid()
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_WithNeitherShippingInput_Returns400()
    {
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Addr None", "addr-none", "admin@addr-none.com");
        var pid = await CreateProductAsync(admin, "None Item", 1m, 10);
        var token = await RegisterAndLoginCustomerAsync(slug, tenantId, "c@addr-none.com");
        var client = CustomerClient(slug, token);

        await client.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });

        var resp = await client.PostAsJsonAsync("/api/v1/storefront/checkout", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_OtherCustomersSavedAddress_Returns404()
    {
        // Customer A saves an address. Customer B (same store) tries to use its id.
        var (slug, tenantId, admin) = await CreateStoreWithAdminAsync("Addr ACL", "addr-acl", "admin@addr-acl.com");
        var pid = await CreateProductAsync(admin, "ACL Item", 1m, 10);

        var aliceToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "alice@addr-acl.com");
        var aliceClient = CustomerClient(slug, aliceToken);
        var aliceAddrResp = await aliceClient.PostAsJsonAsync("/api/v1/storefront/addresses", new { label = "Home", address = AddressBody() });
        var aliceAddr = await aliceAddrResp.Content.ReadFromJsonAsync<CustomerAddressDto>();

        var bobToken = await RegisterAndLoginCustomerAsync(slug, tenantId, "bob@addr-acl.com");
        var bobClient = CustomerClient(slug, bobToken);
        await bobClient.PostAsJsonAsync("/api/v1/storefront/cart/items", new { productId = pid, quantity = 1 });

        var resp = await bobClient.PostAsJsonAsync("/api/v1/storefront/checkout", new
        {
            shippingAddressId = aliceAddr!.Id
        });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
