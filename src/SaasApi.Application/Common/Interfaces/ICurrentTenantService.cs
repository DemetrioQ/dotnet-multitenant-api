namespace SaasApi.Application.Common.Interfaces;

/// <summary>
/// Provides the TenantId for the current HTTP request.
/// Resolved from the X-Tenant-Id header (or JWT claim) by TenantResolutionMiddleware.
/// </summary>
public interface ICurrentTenantService
{
    Guid TenantId { get; }
    bool IsResolved { get; }
}
