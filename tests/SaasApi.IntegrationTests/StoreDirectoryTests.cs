using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.Storefront.Directory;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class StoreDirectoryTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);
    private record TenantDetailResult(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt, string StoreUrl);

    [Fact]
    public async Task GetStores_ReturnsActiveTenantsWithStoreUrl()
    {
        var slug = "dir-active-" + Guid.NewGuid().ToString("N")[..6];
        var resp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Directory Co", slug });
        var tenant = await resp.Content.ReadFromJsonAsync<TenantResult>();
        tenant.Should().NotBeNull();

        var directoryResp = await Client.GetAsync("/api/v1/stores");
        directoryResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await directoryResp.Content.ReadFromJsonAsync<PagedResult<StoreDirectoryItemDto>>();
        var entry = page!.Items.FirstOrDefault(i => i.Slug == slug);
        entry.Should().NotBeNull();
        entry!.Name.Should().Be("Directory Co");
        entry.StoreUrl.Should().Be($"https://{slug}.shop.demetrioq.com");
    }

    [Fact]
    public async Task GetStores_ExcludesInactiveTenants()
    {
        var slug = "dir-inactive-" + Guid.NewGuid().ToString("N")[..6];
        var resp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Hidden Co", slug });
        var tenant = await resp.Content.ReadFromJsonAsync<TenantResult>();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = db.Tenants.IgnoreQueryFilters().First(x => x.Id == tenant!.TenantId);
            t.Deactivate();
            await db.SaveChangesAsync();
        }

        var directoryResp = await Client.GetAsync("/api/v1/stores");
        var page = await directoryResp.Content.ReadFromJsonAsync<PagedResult<StoreDirectoryItemDto>>();
        page!.Items.Should().NotContain(i => i.Slug == slug);
    }

    [Fact]
    public async Task GetStores_RequiresNoAuth()
    {
        // Fresh client, no auth headers, no subdomain host — should still work.
        using var client = Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/stores");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyTenant_IncludesStoreUrl()
    {
        var slug = "url-me-" + Guid.NewGuid().ToString("N")[..6];
        var tenantResp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "My Url Co", slug });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, slug, email: $"user@{slug}.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var meResp = await Client.GetAsync("/api/v1/tenants/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await meResp.Content.ReadFromJsonAsync<TenantDetailResult>();
        body!.StoreUrl.Should().Be($"https://{slug}.shop.demetrioq.com");
    }
}
