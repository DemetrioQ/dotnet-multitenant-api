using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Storefront.Queries.GetStorefrontProducts
{
    public class GetStorefrontProductsHandler(IAppDbContext db)
        : IRequestHandler<GetStorefrontProductsQuery, PagedResult<StorefrontProductDto>>
    {
        public async Task<PagedResult<StorefrontProductDto>> Handle(GetStorefrontProductsQuery request, CancellationToken ct)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

            var active = db.Products.Where(p => p.IsActive);
            var total = await active.CountAsync(ct);

            var items = await active
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new StorefrontProductDto(
                    p.Id, p.Name, p.Slug, p.Description, p.Price, p.Stock, p.ImageUrl, p.Sku))
                .ToListAsync(ct);

            return new PagedResult<StorefrontProductDto>(items, total, page, pageSize);
        }
    }
}
