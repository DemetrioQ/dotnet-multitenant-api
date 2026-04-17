using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class TenantSettingsTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetMyTenant_AfterCreation_ReturnsDefaultSettings()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Settings Co 1", slug = "settings-co-1" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "settings-co-1", "user1@settings.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/api/v1/tenants/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TenantResult>();
        body!.Timezone.Should().Be("UTC");
        body.Currency.Should().Be("USD");
        body.SupportEmail.Should().BeNull();
        body.WebsiteUrl.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_AsAdmin_UpdatesSettingsAndName()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Settings Co 2", slug = "settings-co-2" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await GetAuthTokenAsync(Client, tenant!.TenantId, "settings-co-2", "admin2@settings.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await Client.PutAsJsonAsync($"/api/v1/tenants/{tenant.TenantId}", new
        {
            name = "Settings Co 2 Updated",
            timezone = "America/New_York",
            currency = "EUR",
            supportEmail = "support@acme.com",
            websiteUrl = "https://acme.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TenantResult>();
        body!.Name.Should().Be("Settings Co 2 Updated");
        body.Timezone.Should().Be("America/New_York");
        body.Currency.Should().Be("EUR");
        body.SupportEmail.Should().Be("support@acme.com");
        body.WebsiteUrl.Should().Be("https://acme.com");
    }

    [Fact]
    public async Task UpdateTenant_ReflectsInGetMyTenant()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Settings Co 3", slug = "settings-co-3" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await GetAuthTokenAsync(Client, tenant!.TenantId, "settings-co-3", "admin3@settings.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await Client.PutAsJsonAsync($"/api/v1/tenants/{tenant.TenantId}", new
        {
            name = "Settings Co 3",
            timezone = "Europe/London",
            currency = "GBP",
            supportEmail = (string?)null,
            websiteUrl = (string?)null
        });

        var response = await Client.GetAsync("/api/v1/tenants/me");
        var body = await response.Content.ReadFromJsonAsync<TenantResult>();
        body!.Timezone.Should().Be("Europe/London");
        body.Currency.Should().Be("GBP");
    }

    [Fact]
    public async Task UpdateTenant_AsMember_Returns403()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Settings Co 4", slug = "settings-co-4" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        // Register admin first so the member is the second user (member role)
        await GetAuthTokenAsync(Client, tenant!.TenantId, "settings-co-4", "admin4@settings.com");
        var memberToken = await GetAuthTokenAsync(Client, tenant.TenantId, "settings-co-4", "member4@settings.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var response = await Client.PutAsJsonAsync($"/api/v1/tenants/{tenant.TenantId}", new
        {
            name = "Settings Co 4",
            timezone = "UTC",
            currency = "USD",
            supportEmail = (string?)null,
            websiteUrl = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTenant_InvalidCurrency_Returns400()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Settings Co 5", slug = "settings-co-5" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await GetAuthTokenAsync(Client, tenant!.TenantId, "settings-co-5", "admin5@settings.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await Client.PutAsJsonAsync($"/api/v1/tenants/{tenant.TenantId}", new
        {
            name = "Settings Co 5",
            timezone = "UTC",
            currency = "dollars",
            supportEmail = (string?)null,
            websiteUrl = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyTenant_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/v1/tenants/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TenantResult(Guid TenantId, string Name, string Timezone, string Currency, string? SupportEmail, string? WebsiteUrl);
}
