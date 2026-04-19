using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Features.Payments.Connect;
using SaasApi.Domain.Entities;
using SaasApi.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SaasApi.IntegrationTests;

[Collection("Integration")]
public class PaymentsConnectTests(WebAppFactory factory) : IntegrationTestBase(factory)
{
    private record TenantResult(Guid TenantId);

    private async Task<(Guid tenantId, string adminToken, string slug)> CreateStoreWithAdminAsync(
        string name, string slug, string adminEmail)
    {
        var tenantResp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name, slug });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<TenantResult>();
        var token = await CreateAdminAsync(tenant!.TenantId, slug, adminEmail);
        return (tenant.TenantId, token, slug);
    }

    private HttpClient AdminClient(string token)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    [Fact]
    public async Task Status_WhenNoAccount_ReturnsNotConnected()
    {
        var (_, token, _) = await CreateStoreWithAdminAsync("Connect A", "conn-a", "admin@conn-a.com");

        var resp = await AdminClient(token).GetAsync("/api/v1/payments/connect/status");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<PaymentAccountStatusDto>(JsonOpts);
        body!.Connected.Should().BeFalse();
        body.CanAcceptPayments.Should().BeFalse();
    }

    [Fact]
    public async Task StartOnboarding_CreatesAccount_ReturnsUrl()
    {
        var (_, token, _) = await CreateStoreWithAdminAsync("Connect B", "conn-b", "admin@conn-b.com");
        var client = AdminClient(token);

        var resp = await client.PostAsJsonAsync("/api/v1/payments/connect/onboarding", new
        {
            refreshUrl = "https://saas.demetrioq.com/settings/payments",
            returnUrl = "https://saas.demetrioq.com/settings/payments?done=1"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<StartOnboardingResult>();
        body!.OnboardingUrl.Should().Contain("simulate-onboarding");

        // Status endpoint now shows "connected but pending" since onboarding hasn't completed.
        var statusResp = await client.GetAsync("/api/v1/payments/connect/status");
        var status = await statusResp.Content.ReadFromJsonAsync<PaymentAccountStatusDto>(JsonOpts);
        status!.Connected.Should().BeTrue();
        status.CanAcceptPayments.Should().BeFalse(); // charges_enabled not yet true
    }

    [Fact]
    public async Task SimulatedOnboarding_MarksAccountComplete()
    {
        var (_, token, _) = await CreateStoreWithAdminAsync("Connect C", "conn-c", "admin@conn-c.com");
        var client = AdminClient(token);

        var startResp = await client.PostAsJsonAsync("/api/v1/payments/connect/onboarding", new
        {
            refreshUrl = "https://saas.demetrioq.com/settings/payments",
            returnUrl = "https://saas.demetrioq.com/settings/payments?done=1"
        });
        var start = await startResp.Content.ReadFromJsonAsync<StartOnboardingResult>();

        // Simulated Stripe hosted onboarding completes automatically when we hit the URL.
        // HttpClient follows redirects by default, so just fire-and-forget — the side effect
        // (account synced to complete) is what we're verifying via the next call.
        try { await client.GetAsync(start!.OnboardingUrl); } catch { /* redirect target may 404 */ }

        var statusResp = await client.GetAsync("/api/v1/payments/connect/status");
        var status = await statusResp.Content.ReadFromJsonAsync<PaymentAccountStatusDto>(JsonOpts);
        status!.CanAcceptPayments.Should().BeTrue();
        status.ChargesEnabled.Should().BeTrue();
        status.DetailsSubmitted.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshStatus_PullsLatestFromProvider()
    {
        var (tenantId, token, _) = await CreateStoreWithAdminAsync("Connect D", "conn-d", "admin@conn-d.com");
        var client = AdminClient(token);

        // Seed a pending account directly so we can observe the refresh flipping it to complete.
        await client.PostAsJsonAsync("/api/v1/payments/connect/onboarding", new
        {
            refreshUrl = "https://saas.demetrioq.com/settings/payments",
            returnUrl = "https://saas.demetrioq.com/settings/payments?done=1"
        });

        var refreshResp = await client.PostAsync("/api/v1/payments/connect/refresh-status", null);
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResp.Content.ReadFromJsonAsync<PaymentAccountStatusDto>(JsonOpts);
        refreshed!.CanAcceptPayments.Should().BeTrue(); // simulation provider reports ready
    }

    [Fact]
    public async Task NonAdmin_Cannot_Access_ConnectEndpoints()
    {
        var (tenantId, _, slug) = await CreateStoreWithAdminAsync("Connect E", "conn-e", "admin@conn-e.com");

        var memberToken = await GetAuthTokenAsync(Client, tenantId, slug, email: "member@conn-e.com");
        var memberClient = Factory.CreateClient();
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var resp = await memberClient.GetAsync("/api/v1/payments/connect/status");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
