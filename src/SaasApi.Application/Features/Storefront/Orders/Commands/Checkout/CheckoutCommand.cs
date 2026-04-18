using MediatR;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

public record CheckoutAddressInput(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country);

public record CheckoutCommand(
    CheckoutAddressInput ShippingAddress,
    CheckoutAddressInput? BillingAddress = null) : IRequest<OrderDto>;
