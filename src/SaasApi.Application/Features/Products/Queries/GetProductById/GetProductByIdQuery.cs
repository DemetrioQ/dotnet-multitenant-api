using MediatR;

namespace SaasApi.Application.Features.Products.Queries.GetProductById
{
    public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
}
