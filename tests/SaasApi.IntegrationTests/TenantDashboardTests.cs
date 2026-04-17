using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class TenantDashboardTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetDashboard_ReturnsStats()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Dash Co 1", slug = "dash-co-1" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "dash-co-1", "user1@dash.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product so counts are non-zero
        await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Dash Product",
            description = "Desc",
            price = 5.00,
            stock = 1
        });

        var response = await Client.GetAsync("/api/v1/tenants/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardResult>();
        body!.UserCount.Should().BeGreaterThan(0);
        body.ProductCount.Should().BeGreaterThan(0);
        body.RecentActivity.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDashboard_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/v1/tenants/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TenantResult(Guid TenantId);
    private record RecentActivityEntry(string Action, string EntityType, string? Details, DateTime CreatedAt);
    private record DashboardResult(int UserCount, int ActiveUserCount, int ProductCount, int ActiveProductCount, bool OnboardingComplete, IReadOnlyList<RecentActivityEntry> RecentActivity);
}
