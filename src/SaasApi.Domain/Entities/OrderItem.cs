using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class OrderItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }

    // Snapshot fields — frozen at checkout so later product edits don't rewrite history.
    public string ProductName { get; private set; } = default!;
    public string ProductSlug { get; private set; } = default!;
    public string? ProductSku { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    private OrderItem() { }

    public static OrderItem Create(
        Guid tenantId,
        Guid orderId,
        Product product,
        int quantity)
    {
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));

        return new OrderItem
        {
            TenantId = tenantId,
            OrderId = orderId,
            ProductId = product.Id,
            ProductName = product.Name,
            ProductSlug = product.Slug,
            ProductSku = product.Sku,
            UnitPrice = product.Price,
            Quantity = quantity,
            LineTotal = product.Price * quantity
        };
    }
}
