using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services;

/// <summary>
/// Scoped service — one instance per HTTP request.
/// Set by TenantResolutionMiddleware, read by AppDbContext and handlers.
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    public Guid TenantId { get; private set; }
    public bool IsResolved { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        TenantId = tenantId;
        IsResolved = true;
    }
}
