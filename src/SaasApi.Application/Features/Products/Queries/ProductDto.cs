using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Products.Queries
{
    public record ProductDto(Guid Id, string Name, string Description, decimal Price, int Stock, bool IsActive)
    {
        public static ProductDto FromEntity(Product product) =>
            new(product.Id, product.Name, product.Description, product.Price, product.Stock, product.IsActive);
    }
}
