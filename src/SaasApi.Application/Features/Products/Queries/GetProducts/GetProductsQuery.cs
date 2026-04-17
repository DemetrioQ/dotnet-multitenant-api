using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Products.Queries.GetProducts
{
    public record GetProductsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
}
