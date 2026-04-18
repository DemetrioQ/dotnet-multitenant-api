using MediatR;
using SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.CreateCheckoutSession;

public record CreateCheckoutSessionCommand(
    CheckoutAddressInput ShippingAddress,
    CheckoutAddressInput? BillingAddress,
    string SuccessUrl,
    string CancelUrl) : IRequest<CreateCheckoutSessionResult>;

public record CreateCheckoutSessionResult(
    Guid OrderId,
    string OrderNumber,
    string Provider,
    string SessionId,
    string PaymentUrl);
