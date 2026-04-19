using MediatR;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

public record CheckoutAddressInput(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country);

/// <summary>
/// Shipping/billing addresses can be supplied either inline (CheckoutAddressInput)
/// or as a reference to a saved CustomerAddress (shippingAddressId / billingAddressId).
/// Exactly one of each pair is required. If billing id/inline is null, shipping is used for billing too.
/// </summary>
public record CheckoutCommand(
    CheckoutAddressInput? ShippingAddress = null,
    CheckoutAddressInput? BillingAddress = null,
    Guid? ShippingAddressId = null,
    Guid? BillingAddressId = null) : IRequest<OrderDto>;
