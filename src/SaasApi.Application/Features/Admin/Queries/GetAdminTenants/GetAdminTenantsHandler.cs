using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Admin.Queries.GetAdminTenants;

public class GetAdminTenantsHandler(IAppDbContext db)
    : IRequestHandler<GetAdminTenantsQuery, PagedResult<AdminTenantDto>>
{
    public async Task<PagedResult<AdminTenantDto>> Handle(GetAdminTenantsQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var total = await db.Tenants.IgnoreQueryFilters().CountAsync(ct);
        if (total == 0)
            return new PagedResult<AdminTenantDto>(Array.Empty<AdminTenantDto>(), 0, request.Page, request.PageSize);

        // Page the tenant ids first, then fetch the per-tenant counts for just the page —
        // keeps the GROUP BYs bounded by page size even when the platform grows.
        var pagedTenants = await db.Tenants
            .IgnoreQueryFilters()
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(t => new { t.Id, t.Name, t.Slug, t.IsActive, t.CreatedAt })
            .ToListAsync(ct);

        var pageIds = pagedTenants.Select(t => t.Id).ToHashSet();

        var userCounts = await db.Users.IgnoreQueryFilters()
            .Where(u => pageIds.Contains(u.TenantId))
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var productCounts = await db.Products.IgnoreQueryFilters()
            .Where(p => pageIds.Contains(p.TenantId))
            .GroupBy(p => p.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, ct);

        var lastActivity = await db.AuditLogEntries.IgnoreQueryFilters()
            .Where(a => pageIds.Contains(a.TenantId))
            .GroupBy(a => a.TenantId)
            .Select(g => new { TenantId = g.Key, Last = g.Max(a => a.CreatedAt) })
            .ToDictionaryAsync(x => x.TenantId, x => (DateTime?)x.Last, ct);

        var items = pagedTenants.Select(t => new AdminTenantDto(
            t.Id,
            t.Name,
            t.Slug,
            t.IsActive,
            t.CreatedAt,
            userCounts.TryGetValue(t.Id, out var uc) ? uc : 0,
            productCounts.TryGetValue(t.Id, out var pc) ? pc : 0,
            lastActivity.TryGetValue(t.Id, out var la) ? la : null)).ToList();

        return new PagedResult<AdminTenantDto>(items, total, request.Page, request.PageSize);
    }
}
