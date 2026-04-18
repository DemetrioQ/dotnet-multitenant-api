using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class CartItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private CartItem() { }

    public static CartItem Create(Guid tenantId, Guid cartId, Guid productId, int quantity)
    {
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));

        return new CartItem
        {
            TenantId = tenantId,
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity
        };
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));
        Quantity = quantity;
    }

    public void Increment(int by)
    {
        if (by < 1)
            throw new ArgumentException("Increment must be positive.", nameof(by));
        Quantity += by;
    }
}
