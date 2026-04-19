using MediatR;

namespace SaasApi.Application.Features.Payments.Connect.Commands.RefreshPaymentAccount;

/// <summary>
/// Forces a fresh read from the payment provider, useful right after the merchant
/// returns from the Stripe-hosted onboarding flow (the webhook may not have landed yet).
/// </summary>
public record RefreshPaymentAccountCommand : IRequest<PaymentAccountStatusDto>;
