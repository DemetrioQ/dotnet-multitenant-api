using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Storefront.Queries
{
    public record StorefrontProductDto(
        Guid Id,
        string Name,
        string Slug,
        string Description,
        decimal Price,
        int Stock,
        string? ImageUrl,
        string? Sku)
    {
        public static StorefrontProductDto FromEntity(Product product) =>
            new(
                product.Id,
                product.Name,
                product.Slug,
                product.Description,
                product.Price,
                product.Stock,
                product.ImageUrl,
                product.Sku);
    }
}
