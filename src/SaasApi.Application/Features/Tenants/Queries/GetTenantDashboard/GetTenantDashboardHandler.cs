using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantDashboard;

public class GetTenantDashboardHandler(
    IRepository<User> userRepo,
    IRepository<Product> productRepo,
    IRepository<TenantOnboardingStatus> onboardingRepo,
    IRepository<AuditLogEntry> auditRepo)
    : IRequestHandler<GetTenantDashboardQuery, TenantDashboardDto>
{
    public async Task<TenantDashboardDto> Handle(GetTenantDashboardQuery request, CancellationToken ct)
    {
        var users = await userRepo.GetAllAsync(ct);
        var products = await productRepo.GetAllAsync(ct);

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
            OnboardingComplete: onboardingComplete,
            RecentActivity: recentActivity);
    }
}
