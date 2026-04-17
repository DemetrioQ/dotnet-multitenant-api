using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class OnboardingStatusTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetOnboardingStatus_AfterRegistration_ProfileCompletedTrue()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Onboard Co 1", slug = "onboard-co-1" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var token = await GetAuthTokenAsync(Client, tenant!.TenantId, "onboard-co-1", "user1@onboard.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/api/v1/tenants/onboarding");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OnboardingStatusResult>();
        body!.ProfileCompleted.Should().BeTrue("firstName is required at registration");
        body.FirstProductCreated.Should().BeFalse();
        body.IsComplete.Should().BeFalse("still needs first product");
    }

    [Fact]
    public async Task GetOnboardingStatus_AfterCreateProduct_IsCompleteTrue()
    {
        var tenantResponse = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Onboard Co 3", slug = "onboard-co-3" });
        var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantResult>();

        var adminToken = await CreateAdminAsync(tenant!.TenantId, "onboard-co-3", "admin3@onboard.com");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        await Client.PostAsJsonAsync("/api/v1/products", new
        {
            name = "First Product",
            description = "Desc",
            price = 9.99,
            stock = 10
        });

        var response = await Client.GetAsync("/api/v1/tenants/onboarding");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OnboardingStatusResult>();
        body!.ProfileCompleted.Should().BeTrue();
        body.FirstProductCreated.Should().BeTrue();
        body.IsComplete.Should().BeTrue("both flags are now true");
    }

    [Fact]
    public async Task GetOnboardingStatus_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/v1/tenants/onboarding");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TenantResult(Guid TenantId);
    private record OnboardingStatusResult(bool ProfileCompleted, bool FirstProductCreated, bool IsComplete);
}
