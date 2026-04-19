using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public enum PaymentAccountStatus
{
    Pending = 0,      // account created on Stripe, merchant hasn't finished onboarding
    Complete = 1,     // charges_enabled + details_submitted both true
    Rejected = 2      // Stripe rejected the account (rare in test mode)
}

/// <summary>
/// Each tenant has at most one Stripe Connect account (once they've started onboarding).
/// Absence of this row means the tenant hasn't connected Stripe yet — the storefront's
/// Stripe checkout path returns 503 and their only option is the simple /checkout (COD)
/// flow. Simulation provider ignores this entity entirely.
/// </summary>
public class TenantPaymentAccount : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Provider { get; private set; } = default!;
    public string AccountId { get; private set; } = default!;
    public PaymentAccountStatus Status { get; private set; } = PaymentAccountStatus.Pending;
    public bool ChargesEnabled { get; private set; }
    public bool DetailsSubmitted { get; private set; }

    private TenantPaymentAccount() { }

    public static TenantPaymentAccount Create(Guid tenantId, string provider, string accountId)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("AccountId is required.", nameof(accountId));

        return new TenantPaymentAccount
        {
            TenantId = tenantId,
            Provider = provider,
            AccountId = accountId,
            Status = PaymentAccountStatus.Pending
        };
    }

    public void SyncStatus(bool chargesEnabled, bool detailsSubmitted)
    {
        ChargesEnabled = chargesEnabled;
        DetailsSubmitted = detailsSubmitted;

        Status = (chargesEnabled, detailsSubmitted) switch
        {
            (true, true) => PaymentAccountStatus.Complete,
            _ => PaymentAccountStatus.Pending
        };
    }

    public void MarkRejected()
    {
        Status = PaymentAccountStatus.Rejected;
        ChargesEnabled = false;
    }

    public bool CanAcceptPayments => Status == PaymentAccountStatus.Complete && ChargesEnabled;
}
