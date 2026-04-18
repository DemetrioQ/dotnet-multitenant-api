using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Products.Queries
{
    public record ProductDto(
        Guid Id,
        Guid TenantId,
        string Name,
        string Slug,
        string Description,
        decimal Price,
        int Stock,
        string? ImageUrl,
        string? Sku,
        bool IsActive)
    {
        public static ProductDto FromEntity(Product product) =>
            new(
                product.Id,
                product.TenantId,
                product.Name,
                product.Slug,
                product.Description,
                product.Price,
                product.Stock,
                product.ImageUrl,
                product.Sku,
                product.IsActive);
    }
}
