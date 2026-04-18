using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Storefront.Orders;

public record AddressDto(string Line1, string? Line2, string City, string? Region, string PostalCode, string Country)
{
    public static AddressDto FromEntity(Address a) =>
        new(a.Line1, a.Line2, a.City, a.Region, a.PostalCode, a.Country);
}

public record OrderLineDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? ProductSku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal)
{
    public static OrderLineDto FromEntity(OrderItem i) =>
        new(i.ProductId, i.ProductName, i.ProductSlug, i.ProductSku, i.UnitPrice, i.Quantity, i.LineTotal);
}

public record OrderDto(
    Guid Id,
    string Number,
    string Status,
    decimal Subtotal,
    decimal Total,
    AddressDto ShippingAddress,
    AddressDto BillingAddress,
    IReadOnlyList<OrderLineDto> Items,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? FulfilledAt,
    DateTime? CanceledAt)
{
    public static OrderDto FromEntity(Order order, IEnumerable<OrderItem> items) =>
        new(
            order.Id,
            order.Number,
            order.Status.ToString().ToLowerInvariant(),
            order.Subtotal,
            order.Total,
            AddressDto.FromEntity(order.GetShippingAddress()),
            AddressDto.FromEntity(order.GetBillingAddress()),
            items.Select(OrderLineDto.FromEntity).ToList(),
            order.CreatedAt,
            order.PaidAt,
            order.FulfilledAt,
            order.CanceledAt);
}

public record OrderSummaryDto(
    Guid Id,
    string Number,
    string Status,
    decimal Total,
    int ItemCount,
    DateTime CreatedAt)
{
    public static OrderSummaryDto FromEntity(Order order, int itemCount) =>
        new(
            order.Id,
            order.Number,
            order.Status.ToString().ToLowerInvariant(),
            order.Total,
            itemCount,
            order.CreatedAt);
}
