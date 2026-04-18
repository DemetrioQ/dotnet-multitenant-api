using MediatR;

namespace SaasApi.Application.Features.Products.Commands.CreateProduct
{
    public record CreateProductCommand(
        string Name,
        string Description,
        decimal Price,
        int Stock,
        string? Slug = null,
        string? ImageUrl = null,
        string? Sku = null) : IRequest<CreateProductResult>;

    public record CreateProductResult(Guid ProductId);
}
