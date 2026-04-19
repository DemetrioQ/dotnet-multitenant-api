using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Payments.Connect;

public record PaymentAccountStatusDto(
    bool Connected,
    string Provider,
    string Status,
    bool ChargesEnabled,
    bool DetailsSubmitted,
    bool CanAcceptPayments)
{
    public static PaymentAccountStatusDto NotConnected(string provider) =>
        new(false, provider, PaymentAccountStatus.Pending.ToString().ToLowerInvariant(), false, false, false);

    public static PaymentAccountStatusDto FromEntity(TenantPaymentAccount a) =>
        new(
            Connected: true,
            Provider: a.Provider,
            Status: a.Status.ToString().ToLowerInvariant(),
            ChargesEnabled: a.ChargesEnabled,
            DetailsSubmitted: a.DetailsSubmitted,
            CanAcceptPayments: a.CanAcceptPayments);
}

public record StartOnboardingResult(string OnboardingUrl, DateTime ExpiresAt);
