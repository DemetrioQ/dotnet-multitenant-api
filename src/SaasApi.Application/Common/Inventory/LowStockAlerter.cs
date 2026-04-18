using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Common.Inventory;

/// <summary>
/// Enqueues audit-log "product.low_stock" jobs when a product crosses the configured
/// threshold from above (e.g. 6 → 4 with threshold 5 fires once; 4 → 3 does not).
/// Threshold via Inventory:LowStockThreshold (default 5).
/// </summary>
public static class LowStockAlerter
{
    public static async ValueTask CheckAsync(
        IBackgroundJobQueue jobQueue,
        IConfiguration config,
        IEnumerable<(Product product, int previousStock)> changes,
        CancellationToken ct)
    {
        var threshold = config.GetValue<int?>("Inventory:LowStockThreshold") ?? 5;

        foreach (var (product, previousStock) in changes)
        {
            if (product.Stock <= threshold && previousStock > threshold)
            {
                var productId = product.Id;
                var productName = product.Name;
                var currentStock = product.Stock;
                await jobQueue.EnqueueAsync(async (sp, jobCt) =>
                {
                    var audit = sp.GetRequiredService<IAuditService>();
                    await audit.LogAsync(
                        "product.low_stock",
                        "Product",
                        productId,
                        $"Low stock: '{productName}' is at {currentStock} units (threshold {threshold}).",
                        jobCt);
                }, ct);
            }
        }
    }
}
