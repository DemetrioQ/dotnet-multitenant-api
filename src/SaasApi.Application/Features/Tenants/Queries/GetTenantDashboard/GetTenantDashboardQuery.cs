using MediatR;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantDashboard;

public record GetTenantDashboardQuery : IRequest<TenantDashboardDto>;

public record TopProductDto(Guid ProductId, string Name, string Slug, int UnitsSold, decimal Revenue);

public record TenantDashboardDto(
    int UserCount,
    int ActiveUserCount,
    int ProductCount,
    int ActiveProductCount,
    int CustomerCount,
    int PendingOrderCount,
    int PaidOrderCount,
    // Revenue breakdown: Gross = sum of Order.Total (product price × qty, pre-fee).
    // PlatformFees = snapshot of what the platform took via Stripe application_fee.
    // NetRevenue = what the merchant actually receives (before Stripe processing fees,
    // which are deducted separately by Stripe and visible in their Stripe dashboard).
    decimal GrossRevenue,
    decimal PlatformFees,
    decimal NetRevenue,
    decimal CurrentFeePercent,
    decimal AverageOrderValue,
    IReadOnlyList<TopProductDto> TopProducts,
    bool OnboardingComplete,
    IReadOnlyList<RecentAuditEntryDto> RecentActivity);

public record RecentAuditEntryDto(string Action, string EntityType, string? Details, DateTime CreatedAt);
