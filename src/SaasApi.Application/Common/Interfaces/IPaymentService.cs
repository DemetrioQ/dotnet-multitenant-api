namespace SaasApi.Application.Common.Interfaces;

public record PaymentSessionLineItem(string Name, decimal UnitAmount, int Quantity);

public record CreatePaymentSessionRequest(
    Guid TenantId,
    Guid OrderId,
    string OrderNumber,
    string Currency,
    IReadOnlyList<PaymentSessionLineItem> Items,
    string CustomerEmail,
    string SuccessUrl,
    string CancelUrl,
    string? ConnectedAccountId,
    decimal PlatformFeePercent);

public record PaymentSession(string Provider, string SessionId, string PaymentUrl);

public enum PaymentEventKind
{
    Unknown = 0,
    SessionCompleted,
    SessionExpired,
    AccountUpdated
}

public record PaymentEvent(
    string EventId,
    string SessionId,
    PaymentEventKind Kind,
    string? AccountId = null,
    bool ChargesEnabled = false,
    bool DetailsSubmitted = false);

// ── Connect (multi-tenant payments) ─────────────────────────────────────────

public record CreateConnectAccountResult(string Provider, string AccountId);

public record ConnectOnboardingLink(string Url, DateTime ExpiresAt);

public record ConnectAccountStatusInfo(bool ChargesEnabled, bool DetailsSubmitted);

public interface IPaymentService
{
    string ProviderName { get; }

    Task<PaymentSession> CreateSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct);

    PaymentEvent ParseWebhook(string payload, string? signatureHeader);

    // Simulation provider fakes these out so local dev works without real Stripe Connect setup.
    Task<CreateConnectAccountResult> CreateConnectAccountAsync(
        Guid tenantId, string tenantName, string tenantEmail, CancellationToken ct);

    Task<ConnectOnboardingLink> CreateOnboardingLinkAsync(
        string accountId, string refreshUrl, string returnUrl, CancellationToken ct);

    Task<ConnectAccountStatusInfo> RefreshAccountStatusAsync(
        string accountId, CancellationToken ct);
}
