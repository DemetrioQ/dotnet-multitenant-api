using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Storefront.Queries.GetStorefrontProducts
{
    public record GetStorefrontProductsQuery(int Page = 1, int PageSize = 20)
        : IRequest<PagedResult<StorefrontProductDto>>;
}
