using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Infrastructure.Persistence;
using SaasApi.Infrastructure.Services;
using Serilog.Context;

namespace SaasApi.API.Middleware;

/// <summary>
/// Resolves the current tenant for the request. Precedence:
///   1. JWT "tenant_id" claim (admin/merchant routes).
///   2. Host-based: if Host ends with Storefront:HostSuffix, leftmost label is the tenant slug
///      (cached in IMemoryCache).
///   3. Dev-only fallback: ?storeSlug=acme when Host is localhost.
/// Must run AFTER authentication so context.User is already populated.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    private const string TenantCacheKeyPrefix = "tenant:slug:";
    private static readonly TimeSpan TenantHitTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TenantMissTtl = TimeSpan.FromSeconds(30);

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenantService tenantService,
        AppDbContext db,
        IMemoryCache cache,
        IConfiguration config,
        IHostEnvironment env)
    {
        Guid? tenantId = null;
        string? tenantSlug = null;
        string source = "none";

        var claim = context.User.FindFirst("tenant_id");
        if (claim is not null && Guid.TryParse(claim.Value, out var claimTenantId))
        {
            tenantId = claimTenantId;
            source = "jwt";
        }
        else
        {
            var slug = ExtractSlugFromHost(context, config, env);
            if (slug is not null)
            {
                var resolved = await LookupTenantAsync(slug, db, cache, context.RequestAborted);
                if (resolved is not null)
                {
                    tenantId = resolved;
                    tenantSlug = slug;
                    source = "host";
                }
            }
        }

        if (tenantId.HasValue)
            ((CurrentTenantService)tenantService).SetTenant(tenantId.Value);

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

        using (LogContext.PushProperty("TenantId", tenantId?.ToString() ?? "anonymous"))
        using (LogContext.PushProperty("TenantSlug", tenantSlug ?? "-"))
        using (LogContext.PushProperty("TenantSource", source))
        using (LogContext.PushProperty("UserId", userIdClaim?.Value ?? "anonymous"))
        {
            await next(context);
        }
    }

    private static string? ExtractSlugFromHost(HttpContext context, IConfiguration config, IHostEnvironment env)
    {
        var host = context.Request.Host.Host;

        var suffix = config["Storefront:HostSuffix"];
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            var normalized = suffix.StartsWith('.') ? suffix : "." + suffix;
            if (host.Length > normalized.Length &&
                host.EndsWith(normalized, StringComparison.OrdinalIgnoreCase))
            {
                var head = host[..^normalized.Length];
                if (!head.Contains('.'))
                    return head.ToLowerInvariant();
            }
        }

        if (env.IsDevelopment() &&
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
            context.Request.Query.TryGetValue("storeSlug", out var devSlug) &&
            !string.IsNullOrWhiteSpace(devSlug))
        {
            return devSlug.ToString().ToLowerInvariant();
        }

        return null;
    }

    private static async Task<Guid?> LookupTenantAsync(string slug, AppDbContext db, IMemoryCache cache, CancellationToken ct)
    {
        var key = TenantCacheKeyPrefix + slug;
        if (cache.TryGetValue<Guid?>(key, out var cached))
            return cached;

        var tenantId = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Slug == slug && t.IsActive)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        cache.Set(key, tenantId, tenantId.HasValue ? TenantHitTtl : TenantMissTtl);
        return tenantId;
    }
}
