using SaasApi.Application.Common.Interfaces;
using SaasApi.Infrastructure.Services;

namespace SaasApi.API.Middleware;

/// <summary>
/// Resolves the current tenant from the "tenant_id" claim in the validated JWT.
/// Must run AFTER authentication so the JWT is already validated and context.User is populated.
/// TODO: validate that the tenant exists and is active (query Tenants table or a cache).
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            ((CurrentTenantService)tenantService).SetTenant(tenantId);

        await next(context);
    }
}
