using System.Text.Json;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services.Payments;

/// <summary>
/// A fake payment provider for local dev / portfolio demos. No external calls.
/// The "payment URL" returned points to a dev-only endpoint on the API itself that,
/// when POSTed to, emits a SessionCompleted webhook to the real webhook handler.
/// Webhook signature verification is a no-op; the JSON body is trusted.
/// </summary>
public class SimulationPaymentService : IPaymentService
{
    public string ProviderName => "simulation";

    public Task<PaymentSession> CreateSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct)
    {
        var sessionId = "sim_" + Guid.NewGuid().ToString("N");
        // Customer would normally be redirected to Stripe's hosted page. For simulation,
        // point them to a dev endpoint so the demo works end-to-end.
        var paymentUrl = $"/api/v1/storefront/payments/simulate?sessionId={sessionId}";
        return Task.FromResult(new PaymentSession(ProviderName, sessionId, paymentUrl));
    }

    public PaymentEvent ParseWebhook(string payload, string? signatureHeader)
    {
        var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var eventId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
        var type = root.GetProperty("type").GetString() ?? "";
        var sessionId = root.GetProperty("sessionId").GetString() ?? throw new ArgumentException("sessionId required");

        var kind = type switch
        {
            "session.completed" => PaymentEventKind.SessionCompleted,
            "session.expired" => PaymentEventKind.SessionExpired,
            _ => PaymentEventKind.Unknown
        };

        return new PaymentEvent(eventId, sessionId, kind);
    }
}
