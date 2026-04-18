using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantDashboard;

public class GetTenantDashboardHandler(
    IRepository<User> userRepo,
    IRepository<Product> productRepo,
    IRepository<Customer> customerRepo,
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo,
    IRepository<TenantOnboardingStatus> onboardingRepo,
    IRepository<AuditLogEntry> auditRepo)
    : IRequestHandler<GetTenantDashboardQuery, TenantDashboardDto>
{
    public async Task<TenantDashboardDto> Handle(GetTenantDashboardQuery request, CancellationToken ct)
    {
        var users = await userRepo.GetAllAsync(ct);
        var products = await productRepo.GetAllAsync(ct);
        var customers = await customerRepo.GetAllAsync(ct);
        var orders = await orderRepo.GetAllAsync(ct);

        var paidOrFulfilled = orders
            .Where(o => o.Status is OrderStatus.Paid or OrderStatus.Fulfilled)
            .ToList();

        var totalRevenue = paidOrFulfilled.Sum(o => o.Total);
        var aov = paidOrFulfilled.Count == 0 ? 0m : totalRevenue / paidOrFulfilled.Count;

        var paidOrderIds = paidOrFulfilled.Select(o => o.Id).ToHashSet();
        var paidItems = paidOrderIds.Count == 0
            ? Array.Empty<OrderItem>()
            : (await orderItemRepo.FindAsync(i => paidOrderIds.Contains(i.OrderId), ct)).ToArray();

        var topProducts = paidItems
            .GroupBy(i => i.ProductId)
            .Select(g => new TopProductDto(
                g.Key,
                g.First().ProductName,
                g.First().ProductSlug,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.LineTotal)))
            .OrderByDescending(t => t.Revenue)
            .Take(5)
            .ToList();

        var statuses = await onboardingRepo.FindAsync(_ => true, ct);
        var onboardingComplete = statuses.FirstOrDefault()?.IsComplete ?? false;

        var recentAudit = await auditRepo.GetPagedDescAsync(0, 10, ct);
        var recentActivity = recentAudit
            .Select(a => new RecentAuditEntryDto(a.Action, a.EntityType, a.Details, a.CreatedAt))
            .ToList();

        return new TenantDashboardDto(
            UserCount: users.Count,
            ActiveUserCount: users.Count(u => u.IsActive),
            ProductCount: products.Count,
            ActiveProductCount: products.Count(p => p.IsActive),
            CustomerCount: customers.Count,
            PendingOrderCount: orders.Count(o => o.Status == OrderStatus.Pending),
            PaidOrderCount: paidOrFulfilled.Count,
            TotalRevenue: totalRevenue,
            AverageOrderValue: aov,
            TopProducts: topProducts,
            OnboardingComplete: onboardingComplete,
            RecentActivity: recentActivity);
    }
}
