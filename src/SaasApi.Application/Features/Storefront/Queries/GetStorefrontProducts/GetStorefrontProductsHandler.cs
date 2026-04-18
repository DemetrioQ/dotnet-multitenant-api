using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Queries.GetStorefrontProducts
{
    public class GetStorefrontProductsHandler(IRepository<Product> productRepo)
        : IRequestHandler<GetStorefrontProductsQuery, PagedResult<StorefrontProductDto>>
    {
        public async Task<PagedResult<StorefrontProductDto>> Handle(GetStorefrontProductsQuery request, CancellationToken ct)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

            var active = await productRepo.FindAsync(p => p.IsActive, ct);
            var total = active.Count;
            var items = active
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(StorefrontProductDto.FromEntity)
                .ToList();

            return new PagedResult<StorefrontProductDto>(items, total, page, pageSize);
        }
    }
}
