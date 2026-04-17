using System.Security.Claims;
using Serilog.Context;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Infrastructure.Services;

namespace SaasApi.API.Middleware;

/// <summary>
/// Resolves the current tenant from the "tenant_id" claim in the validated JWT.
/// Must run AFTER authentication so the JWT is already validated and context.User is populated.
/// Also pushes TenantId and UserId into the Serilog LogContext for structured logging.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            ((CurrentTenantService)tenantService).SetTenant(tenantId);

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

        using (LogContext.PushProperty("TenantId", tenantIdClaim?.Value ?? "anonymous"))
        using (LogContext.PushProperty("UserId", userIdClaim?.Value ?? "anonymous"))
        {
            await next(context);
        }
    }
}
