using SaasApi.Application.Common.Interfaces;
using SaasApi.Infrastructure.Services;

namespace SaasApi.API.Middleware;

/// <summary>
/// Reads the X-Tenant-Id header from the incoming request and sets it on the scoped
/// ICurrentTenantService. Must run AFTER authentication so the JWT is already validated.
///
/// TODO: alternatively resolve tenant from JWT claim "tenant_id" instead of a header.
/// TODO: validate that the tenant exists and is active (query Tenants table or a cache).
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValue)
            && Guid.TryParse(tenantIdValue, out var tenantId))
        {
            // CurrentTenantService must be scoped and mutable — see Infrastructure registration
            ((CurrentTenantService)tenantService).SetTenant(tenantId);
        }

        await next(context);
    }
}
