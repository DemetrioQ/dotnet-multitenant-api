using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Directory.Queries.GetStoreDirectory;

public class GetStoreDirectoryHandler(
    IRepository<Tenant> tenantRepo,
    IStoreUrlBuilder storeUrlBuilder)
    : IRequestHandler<GetStoreDirectoryQuery, PagedResult<StoreDirectoryItemDto>>
{
    public async Task<PagedResult<StoreDirectoryItemDto>> Handle(GetStoreDirectoryQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 50 : request.PageSize;

        // Tenant has no query filter (it's the cross-tenant table). Still, use FindGlobal
        // for clarity so future filter changes don't break this.
        var active = await tenantRepo.FindGlobalAsync(t => t.IsActive, ct);
        var ordered = active.OrderBy(t => t.Name).ToList();
        var totalCount = ordered.Count;

        var paged = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new StoreDirectoryItemDto(t.Name, t.Slug, storeUrlBuilder.BuildUrl(t.Slug)))
            .ToList();

        return new PagedResult<StoreDirectoryItemDto>(paged, totalCount, page, pageSize);
    }
}
