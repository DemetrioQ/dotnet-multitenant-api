using MediatR;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantDashboard;

public record GetTenantDashboardQuery : IRequest<TenantDashboardDto>;

public record TenantDashboardDto(
    int UserCount,
    int ActiveUserCount,
    int ProductCount,
    int ActiveProductCount,
    bool OnboardingComplete,
    IReadOnlyList<RecentAuditEntryDto> RecentActivity);

public record RecentAuditEntryDto(string Action, string EntityType, string? Details, DateTime CreatedAt);
