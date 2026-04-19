using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantDashboard;

public class GetTenantDashboardHandler(IAppDbContext db)
    : IRequestHandler<GetTenantDashboardQuery, TenantDashboardDto>
{
    public async Task<TenantDashboardDto> Handle(GetTenantDashboardQuery request, CancellationToken ct)
    {
        // All DbSets are tenant-filtered via global query filter. Strategy: collapse
        // aggregates that target the same table into one GroupBy(_ => 1) query so we
        // trade bytes for round-trips the right way — a single round-trip per source
        // table beats 2–3 per table, especially once network latency is in play.

        // Users: total + active in one round-trip.
        var userStats = await db.Users
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Active = g.Count(u => u.IsActive) })
            .FirstOrDefaultAsync(ct) ?? new { Total = 0, Active = 0 };

        // Products: total + active in one round-trip.
        var productStats = await db.Products
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Active = g.Count(p => p.IsActive) })
            .FirstOrDefaultAsync(ct) ?? new { Total = 0, Active = 0 };

        var customerCount = await db.Customers.CountAsync(ct);

        // Orders: pending + paid/fulfilled counts + paid revenue in one round-trip.
        var orderStats = await db.Orders
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Pending = g.Count(o => o.Status == OrderStatus.Pending),
                Paid = g.Count(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Fulfilled),
                PaidRevenue = g
                    .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Fulfilled)
                    .Sum(o => (decimal?)o.Total) ?? 0m
            })
            .FirstOrDefaultAsync(ct)
            ?? new { Pending = 0, Paid = 0, PaidRevenue = 0m };

        var aov = orderStats.Paid == 0 ? 0m : orderStats.PaidRevenue / orderStats.Paid;

        // Top 5 products by revenue — SQL GROUP BY over OrderItems of paid/fulfilled orders.
        var paidOrderIds = db.Orders
            .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Fulfilled)
            .Select(o => o.Id);
        // SQLite can't ORDER BY a decimal expression (works on SQL Server). Cast the
        // sum to double only for the ordering; the projected value stays decimal.
        var topProducts = await db.OrderItems
            .Where(i => paidOrderIds.Contains(i.OrderId))
            .GroupBy(i => new { i.ProductId, i.ProductName, i.ProductSlug })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ProductSlug,
                Units = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal),
                RevenueForSort = (double)g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.RevenueForSort)
            .Take(5)
            .Select(x => new TopProductDto(x.ProductId, x.ProductName, x.ProductSlug, x.Units, x.Revenue))
            .ToListAsync(ct);

        var onboardingComplete = await db.TenantOnboardingStatuses
            .Select(s => (bool?)s.IsComplete)
            .FirstOrDefaultAsync(ct) ?? false;

        var recentActivity = await db.AuditLogEntries
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentAuditEntryDto(a.Action, a.EntityType, a.Details, a.CreatedAt))
            .ToListAsync(ct);

        return new TenantDashboardDto(
            UserCount: userStats.Total,
            ActiveUserCount: userStats.Active,
            ProductCount: productStats.Total,
            ActiveProductCount: productStats.Active,
            CustomerCount: customerCount,
            PendingOrderCount: orderStats.Pending,
            PaidOrderCount: orderStats.Paid,
            TotalRevenue: orderStats.PaidRevenue,
            AverageOrderValue: aov,
            TopProducts: topProducts,
            OnboardingComplete: onboardingComplete,
            RecentActivity: recentActivity);
    }
}
