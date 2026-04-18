namespace SaasApi.Application.Features.Storefront.Cart;

public record CartLineDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int AvailableStock);

public record CartDto(
    Guid CartId,
    IReadOnlyList<CartLineDto> Items,
    decimal Subtotal,
    int TotalItems);
