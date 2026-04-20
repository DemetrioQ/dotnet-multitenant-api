using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Payments.Connect;

public record PaymentAccountStatusDto(
    bool Connected,
    string Provider,
    string Status,
    bool ChargesEnabled,
    bool DetailsSubmitted,
    bool CanAcceptPayments,
    decimal PlatformFeePercent)
{
    public static PaymentAccountStatusDto NotConnected(string provider, decimal platformFeePercent) =>
        new(false, provider, PaymentAccountStatus.Pending.ToString().ToLowerInvariant(),
            false, false, false, platformFeePercent);

    public static PaymentAccountStatusDto FromEntity(TenantPaymentAccount a, decimal platformFeePercent) =>
        new(
            Connected: true,
            Provider: a.Provider,
            Status: a.Status.ToString().ToLowerInvariant(),
            ChargesEnabled: a.ChargesEnabled,
            DetailsSubmitted: a.DetailsSubmitted,
            CanAcceptPayments: a.CanAcceptPayments,
            PlatformFeePercent: platformFeePercent);
}

public record StartOnboardingResult(string OnboardingUrl, DateTime ExpiresAt);
