using MediatR;

namespace SaasApi.Application.Features.Products.Commands.CreateProduct
{
    public record CreateProductCommand(string Name, string Description, decimal Price, int Stock) : IRequest<CreateProductResult>;

    public record CreateProductResult(Guid ProductId);
}
