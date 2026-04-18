using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities
{
    public class Product : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; private set; }
        public string Name { get; private set; } = default!;
        public string Slug { get; private set; } = default!;
        public string Description { get; private set; } = default!;
        public decimal Price { get; private set; }
        public int Stock { get; private set; }
        public string? ImageUrl { get; private set; }
        public string? Sku { get; private set; }
        public bool IsActive { get; private set; } = true;

        private Product() { }

        public static Product Create(
            Guid tenantId,
            string name,
            string slug,
            string description,
            decimal price,
            int stock,
            string? imageUrl = null,
            string? sku = null)
        {
            return new Product
            {
                TenantId = tenantId,
                Name = name,
                Slug = slug,
                Description = description,
                Price = price,
                Stock = stock,
                ImageUrl = imageUrl,
                Sku = sku
            };
        }

        public void Update(string name, string description, decimal price, int stock)
        {
            Name = name;
            Description = description;
            Price = price;
            Stock = stock;
        }

        public void UpdateSlug(string slug) => Slug = slug;
        public void UpdateImageUrl(string? imageUrl) => ImageUrl = imageUrl;
        public void UpdateSku(string? sku) => Sku = sku;

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;
    }
}
