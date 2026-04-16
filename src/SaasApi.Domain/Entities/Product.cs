using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities
{
    public class Product : BaseEntity, ITenantEntity
    {
        public Guid TenantId { get; private set; } // needed for the global query filter
        public string Name { get; private set; }
        public string Description { get; private set; }
        public decimal Price { get; private set; }
        public int Stock { get; private set; }
        public bool IsActive { get; private set; } = true;

        private Product() { } // EF Core

        public static Product Create(Guid tenantId, string name, string description, decimal price, int stock)
        {
            return new Product { TenantId = tenantId, Name = name, Description = description, Price = price, Stock = stock };
        }

        public void Update(string name, string description, decimal price, int stock)
        {
            Name = name;
            Description = description;
            Price = price;
            Stock = stock;
        }

        public void Deactivate() => IsActive = false;
    }
}
