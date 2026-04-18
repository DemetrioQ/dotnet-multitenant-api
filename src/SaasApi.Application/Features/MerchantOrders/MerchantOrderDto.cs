using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.MerchantOrders;

public record MerchantOrderLineDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? ProductSku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal)
{
    public static MerchantOrderLineDto FromEntity(OrderItem i) =>
        new(i.ProductId, i.ProductName, i.ProductSlug, i.ProductSku, i.UnitPrice, i.Quantity, i.LineTotal);
}

public record MerchantOrderCustomerDto(Guid Id, string Email, string FirstName, string LastName);

public record MerchantOrderDto(
    Guid Id,
    string Number,
    string Status,
    MerchantOrderCustomerDto Customer,
    decimal Subtotal,
    decimal Total,
    string? PaymentProvider,
    string ShippingLine1,
    string? ShippingLine2,
    string ShippingCity,
    string? ShippingRegion,
    string ShippingPostalCode,
    string ShippingCountry,
    IReadOnlyList<MerchantOrderLineDto> Items,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? FulfilledAt,
    DateTime? CanceledAt)
{
    public static MerchantOrderDto FromEntity(Order order, Customer customer, IEnumerable<OrderItem> items) =>
        new(
            order.Id,
            order.Number,
            order.Status.ToString().ToLowerInvariant(),
            new MerchantOrderCustomerDto(customer.Id, customer.Email, customer.FirstName, customer.LastName),
            order.Subtotal,
            order.Total,
            order.PaymentProvider,
            order.ShippingLine1,
            order.ShippingLine2,
            order.ShippingCity,
            order.ShippingRegion,
            order.ShippingPostalCode,
            order.ShippingCountry,
            items.Select(MerchantOrderLineDto.FromEntity).ToList(),
            order.CreatedAt,
            order.PaidAt,
            order.FulfilledAt,
            order.CanceledAt);
}

public record MerchantOrderSummaryDto(
    Guid Id,
    string Number,
    string Status,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    decimal Total,
    int ItemCount,
    DateTime CreatedAt)
{
    public static MerchantOrderSummaryDto FromEntity(Order order, Customer customer, int itemCount) =>
        new(
            order.Id,
            order.Number,
            order.Status.ToString().ToLowerInvariant(),
            customer.Id,
            customer.Email,
            $"{customer.FirstName} {customer.LastName}",
            order.Total,
            itemCount,
            order.CreatedAt);
}
