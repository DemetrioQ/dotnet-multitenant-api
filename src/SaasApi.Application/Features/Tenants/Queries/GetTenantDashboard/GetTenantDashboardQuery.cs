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
    decimal TotalRevenue,
    decimal AverageOrderValue,
    IReadOnlyList<TopProductDto> TopProducts,
    bool OnboardingComplete,
    IReadOnlyList<RecentAuditEntryDto> RecentActivity);

public record RecentAuditEntryDto(string Action, string EntityType, string? Details, DateTime CreatedAt);
