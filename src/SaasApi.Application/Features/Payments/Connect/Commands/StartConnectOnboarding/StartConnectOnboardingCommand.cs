using MediatR;

namespace SaasApi.Application.Features.Payments.Connect.Commands.StartConnectOnboarding;

/// <summary>
/// Merchant clicks "Connect Stripe" in the dashboard. We create (or reuse) a connected
/// Stripe account for this tenant and hand back a short-lived Stripe-hosted onboarding URL
/// for the merchant to fill in bank info, tax id, etc. RefreshUrl and ReturnUrl are where
/// Stripe sends the merchant after the flow (or if the link expires before they finish).
/// </summary>
public record StartConnectOnboardingCommand(
    string RefreshUrl,
    string ReturnUrl) : IRequest<StartOnboardingResult>;
