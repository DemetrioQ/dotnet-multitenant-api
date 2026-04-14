namespace SaasApi.Application.Features.Products.Queries
{
    public record ProductDto(Guid Id, string Name, string Description, decimal Price, int Stock);
}
