using MediatR;
using SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.CreateCheckoutSession;

/// <summary>
/// Same address rules as CheckoutCommand: shipping and billing each accept either an inline
/// CheckoutAddressInput or a saved CustomerAddress id. Shipping is required (inline or id).
/// Billing defaults to shipping when both options are null.
/// </summary>
public record CreateCheckoutSessionCommand(
    string SuccessUrl,
    string CancelUrl,
    CheckoutAddressInput? ShippingAddress = null,
    CheckoutAddressInput? BillingAddress = null,
    Guid? ShippingAddressId = null,
    Guid? BillingAddressId = null) : IRequest<CreateCheckoutSessionResult>;

public record CreateCheckoutSessionResult(
    Guid OrderId,
    string OrderNumber,
    string Provider,
    string SessionId,
    string PaymentUrl);
