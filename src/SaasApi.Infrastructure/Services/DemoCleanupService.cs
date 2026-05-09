using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaasApi.Infrastructure.Persistence;

namespace SaasApi.Infrastructure.Services;

/// <summary>
/// Periodically purges expired demo tenants and demo customers along with their
/// dependent rows. Tenant-scoped tables are deleted in dependency order via
/// ExecuteDeleteAsync — keeps the operation as a few SQL DELETEs rather than
/// loading entities into memory.
/// </summary>
public class DemoCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // First sweep on startup, then on the interval.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo cleanup sweep failed.");
            }

            try { await Task.Delay(SweepInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // 1) Demo tenants — purge tenant + all dependent tenant-scoped rows.
        var expiredTenantIds = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.IsDemo && t.DemoExpiresAt != null && t.DemoExpiresAt < now)
            .Select(t => t.Id)
            .ToListAsync(ct);

        foreach (var tenantId in expiredTenantIds)
        {
            await PurgeTenantAsync(db, tenantId, ct);
            logger.LogInformation("Purged expired demo tenant {TenantId}", tenantId);
        }

        // 2) Demo customers in non-demo tenants — defensive in case we ever add a
        //    curated showcase tenant. In the all-demo-tenant model, customers
        //    cascade with their tenant above.
        await PurgeOrphanDemoCustomersAsync(db, now, ct);
    }

    private static async Task PurgeTenantAsync(AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        // Order matters: child rows first, then parents. ExecuteDeleteAsync runs
        // a single DELETE per call; bypasses change tracker and query filters.
        await db.OrderItems.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.Orders.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.CartItems.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.Carts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.CustomerAddresses.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.CustomerRefreshTokens.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.CustomerEmailVerificationTokens.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.CustomerPasswordResetTokens.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.Customers.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);

        await db.RefreshTokens.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.EmailVerificationTokens.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.PasswordResetTokens.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.UserProfiles.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.AuditLogEntries.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.Invitations.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.Users.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);

        await db.Products.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);

        await db.OAuthAuthorizationCodes.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.OAuthRefreshTokens.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.OAuthClients.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);

        await db.TenantSettings.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.TenantOnboardingStatuses.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.TenantEmailTemplates.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);
        await db.TenantPaymentAccounts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(ct);

        await db.Tenants.IgnoreQueryFilters().Where(x => x.Id == tenantId).ExecuteDeleteAsync(ct);
    }

    private static async Task PurgeOrphanDemoCustomersAsync(AppDbContext db, DateTime now, CancellationToken ct)
    {
        var expiredCustomerIds = await db.Customers
            .IgnoreQueryFilters()
            .Where(c => c.IsDemo && c.DemoExpiresAt != null && c.DemoExpiresAt < now)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (expiredCustomerIds.Count == 0) return;

        var idSet = expiredCustomerIds.ToHashSet();

        await db.OrderItems.IgnoreQueryFilters()
            .Where(oi => db.Orders.IgnoreQueryFilters()
                .Where(o => idSet.Contains(o.CustomerId))
                .Select(o => o.Id).Contains(oi.OrderId))
            .ExecuteDeleteAsync(ct);
        await db.Orders.IgnoreQueryFilters().Where(o => idSet.Contains(o.CustomerId)).ExecuteDeleteAsync(ct);
        await db.CartItems.IgnoreQueryFilters()
            .Where(ci => db.Carts.IgnoreQueryFilters()
                .Where(c => idSet.Contains(c.CustomerId))
                .Select(c => c.Id).Contains(ci.CartId))
            .ExecuteDeleteAsync(ct);
        await db.Carts.IgnoreQueryFilters().Where(c => idSet.Contains(c.CustomerId)).ExecuteDeleteAsync(ct);
        await db.CustomerAddresses.IgnoreQueryFilters().Where(a => idSet.Contains(a.CustomerId)).ExecuteDeleteAsync(ct);
        await db.CustomerRefreshTokens.IgnoreQueryFilters().Where(t => idSet.Contains(t.CustomerId)).ExecuteDeleteAsync(ct);
        await db.CustomerEmailVerificationTokens.IgnoreQueryFilters().Where(t => idSet.Contains(t.CustomerId)).ExecuteDeleteAsync(ct);
        await db.CustomerPasswordResetTokens.IgnoreQueryFilters().Where(t => idSet.Contains(t.CustomerId)).ExecuteDeleteAsync(ct);
        await db.Customers.IgnoreQueryFilters().Where(c => idSet.Contains(c.Id)).ExecuteDeleteAsync(ct);
    }
}
