using MediatR;

namespace SaasApi.Application.Features.Products.Queries.GetProducts
{
    public record GetProductsQuery(int Page, int PageSize) : IRequest<GetProductsResult>;

    public record GetProductsResult(IReadOnlyList<ProductDto> Products, int TotalCount);
}
