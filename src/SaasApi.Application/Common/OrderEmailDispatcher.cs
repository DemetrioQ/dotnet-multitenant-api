using Microsoft.Extensions.DependencyInjection;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Common;

/// <summary>
/// Queues a tenant-templated order lifecycle email. Called after order state transitions
/// so the email content reflects committed DB state.
/// </summary>
public static class OrderEmailDispatcher
{
    public static async ValueTask EnqueueAsync(
        IBackgroundJobQueue jobQueue,
        EmailTemplateType type,
        Order order,
        Customer customer,
        string storeName,
        string storeUrl,
        CancellationToken ct)
    {
        var trimmedStoreUrl = storeUrl.TrimEnd('/');
        var model = new OrderEmailModel(
            storeName,
            trimmedStoreUrl,
            customer.FirstName,
            customer.Email,
            order.Number,
            order.Total,
            $"{trimmedStoreUrl}/orders/{order.Id}");

        var tenantId = order.TenantId;
        var email = customer.Email;

        await jobQueue.EnqueueAsync(async (sp, jobCt) =>
        {
            var svc = sp.GetRequiredService<IEmailService>();
            await svc.SendTenantEmailAsync(tenantId, storeName, email, type, model, jobCt);
        }, ct);
    }
}
