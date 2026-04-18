namespace SaasApi.Application.Common.Interfaces;

public record PaymentSessionLineItem(string Name, decimal UnitAmount, int Quantity);

public record CreatePaymentSessionRequest(
    Guid OrderId,
    string OrderNumber,
    string Currency,
    IReadOnlyList<PaymentSessionLineItem> Items,
    string CustomerEmail,
    string SuccessUrl,
    string CancelUrl);

public record PaymentSession(string Provider, string SessionId, string PaymentUrl);

public enum PaymentEventKind
{
    Unknown = 0,
    SessionCompleted,
    SessionExpired
}

public record PaymentEvent(string EventId, string SessionId, PaymentEventKind Kind);

public interface IPaymentService
{
    /// <summary>Provider name as stored on the order ("simulation" or "stripe").</summary>
    string ProviderName { get; }

    Task<PaymentSession> CreateSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct);

    /// <summary>
    /// Verifies the incoming webhook payload's signature and parses it into a PaymentEvent.
    /// Throws UnauthorizedAccessException on invalid signature.
    /// </summary>
    PaymentEvent ParseWebhook(string payload, string? signatureHeader);
}
