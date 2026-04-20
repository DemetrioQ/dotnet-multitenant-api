using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Application.Features.Admin.Queries.GetPlatformStats;

public class GetPlatformStatsHandler(IAppDbContext db)
    : IRequestHandler<GetPlatformStatsQuery, PlatformStatsDto>
{
    public async Task<PlatformStatsDto> Handle(GetPlatformStatsQuery request, CancellationToken ct)
    {
        // Superadmin view — bypass the tenant filter on the tenant-scoped tables and
        // run SQL-side counts instead of loading every row into memory.
        var since = DateTime.UtcNow.AddDays(-7);

        var tenantStats = await db.Tenants
            .IgnoreQueryFilters()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(t => t.IsActive),
                Inactive = g.Count(t => !t.IsActive),
                NewThisWeek = g.Count(t => t.CreatedAt >= since)
            })
            .FirstOrDefaultAsync(ct)
            ?? new { Total = 0, Active = 0, Inactive = 0, NewThisWeek = 0 };

        var totalUsers = await db.Users.IgnoreQueryFilters().CountAsync(ct);
        var totalProducts = await db.Products.IgnoreQueryFilters().CountAsync(ct);

        return new PlatformStatsDto(
            TotalTenants: tenantStats.Total,
            ActiveTenants: tenantStats.Active,
            InactiveTenants: tenantStats.Inactive,
            NewTenantsThisWeek: tenantStats.NewThisWeek,
            TotalUsers: totalUsers,
            TotalProducts: totalProducts);
    }
}
