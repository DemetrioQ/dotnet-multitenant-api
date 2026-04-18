using MediatR;

namespace SaasApi.Application.Features.Products.Commands.UpdateProduct
{
    public record UpdateProductCommand(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        int Stock,
        string? Slug = null,
        string? ImageUrl = null,
        string? Sku = null) : IRequest<UpdateProductResult>;

    public record UpdateProductResult(Guid ProductId);
}
