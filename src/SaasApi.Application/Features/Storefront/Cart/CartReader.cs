using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Cart;

/// <summary>
/// Builds a CartDto by joining the cart's line items with the current state of Products.
/// The cart itself does not snapshot product name/price — those are read live.
/// Snapshotting happens at checkout (Phase 2C).
/// </summary>
internal static class CartReader
{
    public static async Task<CartDto> BuildAsync(
        Domain.Entities.Cart cart,
        IRepository<CartItem> itemRepo,
        IRepository<Product> productRepo,
        CancellationToken ct)
    {
        var items = await itemRepo.FindAsync(i => i.CartId == cart.Id, ct);
        return await BuildAsync(cart, items, productRepo, ct);
    }

    public static async Task<CartDto> BuildAsync(
        Domain.Entities.Cart cart,
        IReadOnlyList<CartItem> items,
        IRepository<Product> productRepo,
        CancellationToken ct)
    {
        if (items.Count == 0)
            return new CartDto(cart.Id, Array.Empty<CartLineDto>(), 0m, 0);

        var productIds = items.Select(i => i.ProductId).ToHashSet();
        var products = await productRepo.FindAsync(p => productIds.Contains(p.Id), ct);
        var productsById = products.ToDictionary(p => p.Id);

        var lines = new List<CartLineDto>(items.Count);
        decimal subtotal = 0m;
        int totalItems = 0;

        foreach (var item in items)
        {
            if (!productsById.TryGetValue(item.ProductId, out var product) || !product.IsActive)
                continue;

            var lineTotal = product.Price * item.Quantity;
            subtotal += lineTotal;
            totalItems += item.Quantity;

            lines.Add(new CartLineDto(
                product.Id,
                product.Name,
                product.Slug,
                product.ImageUrl,
                product.Price,
                item.Quantity,
                lineTotal,
                product.Stock));
        }

        return new CartDto(cart.Id, lines, subtotal, totalItems);
    }
}
