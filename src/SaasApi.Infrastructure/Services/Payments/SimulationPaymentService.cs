using System.Text.Json;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services.Payments;

/// <summary>
/// Local-dev / portfolio-demo payment provider. Every Stripe-facing method is faked:
/// sessions point to a dev endpoint, Connect onboarding auto-marks accounts complete.
/// </summary>
public class SimulationPaymentService : IPaymentService
{
    public string ProviderName => "simulation";

    public Task<PaymentSession> CreateSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct)
    {
        var sessionId = "sim_" + Guid.NewGuid().ToString("N");
        var paymentUrl = $"/api/v1/storefront/payments/simulate?sessionId={sessionId}";
        return Task.FromResult(new PaymentSession(ProviderName, sessionId, paymentUrl));
    }

    public PaymentEvent ParseWebhook(string payload, string? signatureHeader)
    {
        var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var eventId = root.TryGetProperty("id", out var idProp)
            ? idProp.GetString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();
        var type = root.GetProperty("type").GetString() ?? "";

        if (type == "account.updated")
        {
            var accountId = root.TryGetProperty("accountId", out var aIdProp) ? aIdProp.GetString() ?? "" : "";
            var charges = root.TryGetProperty("chargesEnabled", out var c) && c.GetBoolean();
            var details = root.TryGetProperty("detailsSubmitted", out var d) && d.GetBoolean();
            return new PaymentEvent(eventId, "", PaymentEventKind.AccountUpdated, accountId, charges, details);
        }

        var sessionId = root.TryGetProperty("sessionId", out var sIdProp)
            ? sIdProp.GetString() ?? ""
            : "";

        var kind = type switch
        {
            "session.completed" => PaymentEventKind.SessionCompleted,
            "session.expired" => PaymentEventKind.SessionExpired,
            _ => PaymentEventKind.Unknown
        };

        return new PaymentEvent(eventId, sessionId, kind);
    }

    public Task<CreateConnectAccountResult> CreateConnectAccountAsync(
        Guid tenantId, string tenantName, string tenantEmail, CancellationToken ct)
    {
        var accountId = "sim_acct_" + tenantId.ToString("N")[..12];
        return Task.FromResult(new CreateConnectAccountResult(ProviderName, accountId));
    }

    public Task<ConnectOnboardingLink> CreateOnboardingLinkAsync(
        string accountId, string refreshUrl, string returnUrl, CancellationToken ct)
    {
        // Points to a dev-only endpoint that auto-completes the account then redirects
        // back to returnUrl — mimics Stripe's hosted onboarding without a real setup.
        var url = $"/api/v1/payments/connect/simulate-onboarding?accountId={accountId}" +
                  $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Task.FromResult(new ConnectOnboardingLink(url, DateTime.UtcNow.AddMinutes(10)));
    }

    public Task<ConnectAccountStatusInfo> RefreshAccountStatusAsync(string accountId, CancellationToken ct) =>
        Task.FromResult(new ConnectAccountStatusInfo(ChargesEnabled: true, DetailsSubmitted: true));
}
