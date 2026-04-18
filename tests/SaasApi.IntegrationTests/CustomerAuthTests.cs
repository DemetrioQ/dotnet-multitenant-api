using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class CustomerAuthTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);
    private record RegisterResult(Guid CustomerId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);

    private HttpClient ClientForSubdomain(string slug)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Host = $"{slug}.shop.demetrioq.com";
        return c;
    }

    private async Task<(HttpClient client, string slug, Guid tenantId)> CreateStoreAsync(string name, string slug)
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name, slug });
        var tenant = await resp.Content.ReadFromJsonAsync<TenantResult>();
        return (ClientForSubdomain(slug), slug, tenant!.TenantId);
    }

    private async Task VerifyCustomerAsync(Guid tenantId, string email)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customer = db.Customers.IgnoreQueryFilters()
            .First(c => c.Email == email && c.TenantId == tenantId);
        customer.VerifyEmail();
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Register_ValidSubdomain_Returns201()
    {
        var (client, _, _) = await CreateStoreAsync("Register Co", "cust-reg");

        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email = "alice@example.com",
            password = "Password1!",
            firstName = "Alice",
            lastName = "Doe"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_UnknownSubdomain_Returns404()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Host = "no-such-store-xyz.shop.demetrioq.com";

        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email = "ghost@example.com",
            password = "Password1!",
            firstName = "Ghost",
            lastName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var (client, _, _) = await CreateStoreAsync("Dupe Co", "cust-dupe");

        var body = new { email = "dupe@example.com", password = "Password1!", firstName = "A", lastName = "B" };
        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", body);
        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_Unverified_Returns403()
    {
        var (client, _, _) = await CreateStoreAsync("Unverified Co", "cust-unv");

        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email = "unv@example.com",
            password = "Password1!",
            firstName = "Un",
            lastName = "Verified"
        });

        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/login", new
        {
            email = "unv@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_VerifiedCustomer_ReturnsJwt()
    {
        var (client, _, tenantId) = await CreateStoreAsync("Login Co", "cust-login");

        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email = "ver@example.com",
            password = "Password1!",
            firstName = "Ver",
            lastName = "Customer"
        });
        await VerifyCustomerAsync(tenantId, "ver@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/login", new
        {
            email = "ver@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        result!.JwtToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (client, _, tenantId) = await CreateStoreAsync("BadPw Co", "cust-badpw");

        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email = "pw@example.com",
            password = "Password1!",
            firstName = "Bad",
            lastName = "Pw"
        });
        await VerifyCustomerAsync(tenantId, "pw@example.com");

        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/login", new
        {
            email = "pw@example.com",
            password = "WrongPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_CustomerFromOtherStore_CannotLoginHere()
    {
        var (clientA, _, tenantA) = await CreateStoreAsync("Iso A", "cust-iso-a");
        var (clientB, _, _)      = await CreateStoreAsync("Iso B", "cust-iso-b");

        // Customer registers at store A.
        await clientA.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email = "shared@example.com",
            password = "Password1!",
            firstName = "Cross",
            lastName = "Tenant"
        });
        await VerifyCustomerAsync(tenantA, "shared@example.com");

        // Tries to log into store B with the same credentials.
        var response = await clientB.PostAsJsonAsync("/api/v1/storefront/auth/login", new
        {
            email = "shared@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_MarksCustomerVerified()
    {
        var (client, _, tenantId) = await CreateStoreAsync("Verify Co", "cust-ver");

        await client.PostAsJsonAsync("/api/v1/storefront/auth/register", new
        {
            email = "tok@example.com",
            password = "Password1!",
            firstName = "Tok",
            lastName = "Customer"
        });

        string token;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var vt = db.CustomerEmailVerificationTokens.IgnoreQueryFilters()
                .First(t => t.TenantId == tenantId);
            token = vt.Token;
        }

        var response = await client.GetAsync($"/api/v1/storefront/auth/verify-email?token={token}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = db.Customers.IgnoreQueryFilters()
                .First(c => c.Email == "tok@example.com" && c.TenantId == tenantId);
            customer.IsEmailVerified.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ForgotPassword_AlwaysReturns200()
    {
        var (client, _, _) = await CreateStoreAsync("Forgot Co", "cust-forgot");

        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/forgot-password", new
        {
            email = "does-not-exist@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_UnknownToken_Returns404()
    {
        var client = ClientForSubdomain("cust-reset-any");

        var response = await client.PostAsJsonAsync("/api/v1/storefront/auth/reset-password", new
        {
            token = "bogus-token-value",
            newPassword = "NewPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
