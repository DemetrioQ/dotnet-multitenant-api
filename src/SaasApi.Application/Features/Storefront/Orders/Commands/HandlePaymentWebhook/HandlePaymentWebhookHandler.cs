using MediatR;
using SaasApi.Application.Common;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.HandlePaymentWebhook;

public class HandlePaymentWebhookHandler(
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo,
    IRepository<Product> productRepo,
    IRepository<CartItem> cartItemRepo,
    IRepository<Domain.Entities.Cart> cartRepo,
    IRepository<Customer> customerRepo,
    IRepository<Tenant> tenantRepo,
    IRepository<TenantPaymentAccount> paymentAccountRepo,
    IStoreUrlBuilder storeUrlBuilder,
    IBackgroundJobQueue jobQueue,
    IPaymentService paymentService,
    IAuditService auditService)
    : IRequestHandler<HandlePaymentWebhookCommand>
{
    public async Task Handle(HandlePaymentWebhookCommand request, CancellationToken ct)
    {
        var evt = paymentService.ParseWebhook(request.Payload, request.SignatureHeader);

        if (evt.Kind == PaymentEventKind.Unknown) return;

        switch (evt.Kind)
        {
            case PaymentEventKind.SessionCompleted:
                await HandleSessionEventAsync(evt.SessionId, HandleSessionCompletedAsync, ct);
                break;
            case PaymentEventKind.SessionExpired:
                await HandleSessionEventAsync(evt.SessionId, HandleSessionExpiredAsync, ct);
                break;
            case PaymentEventKind.AccountUpdated:
                await HandleAccountUpdatedAsync(evt, ct);
                break;
        }
    }

    private async Task HandleSessionEventAsync(string sessionId, Func<Order, CancellationToken, Task> handler, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;

        // Webhook is not tenant-scoped — look up order globally by session id.
        var orders = await orderRepo.FindGlobalAsync(o => o.PaymentSessionId == sessionId, ct);
        var order = orders.FirstOrDefault();
        if (order is null) return;

        await handler(order, ct);
    }

    private async Task HandleSessionCompletedAsync(Order order, CancellationToken ct)
    {
        if (order.Status != OrderStatus.Pending) return; // idempotent: already handled

        order.MarkPaid();
        orderRepo.Update(order);

        var carts = await cartRepo.FindGlobalAsync(c => c.CustomerId == order.CustomerId, ct);
        var cart = carts.FirstOrDefault();
        if (cart is not null)
        {
            var items = await cartItemRepo.FindGlobalAsync(i => i.CartId == cart.Id, ct);
            foreach (var i in items) cartItemRepo.Remove(i);
        }

        await orderRepo.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "order.paid",
            "Order",
            order.Id,
            $"Order {order.Number} marked paid via {order.PaymentProvider}.",
            ct);

        await EnqueueOrderEmailAsync(EmailTemplateType.OrderPaid, order, ct);
    }

    private async Task HandleSessionExpiredAsync(Order order, CancellationToken ct)
    {
        if (order.Status != OrderStatus.Pending) return;

        var items = await orderItemRepo.FindGlobalAsync(i => i.OrderId == order.Id, ct);
        foreach (var item in items)
        {
            var products = await productRepo.FindGlobalAsync(p => p.Id == item.ProductId, ct);
            var product = products.FirstOrDefault();
            if (product is null) continue;

            product.Update(product.Name, product.Description, product.Price, product.Stock + item.Quantity);
            productRepo.Update(product);
        }

        order.Cancel();
        orderRepo.Update(order);
        await orderRepo.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "order.canceled",
            "Order",
            order.Id,
            $"Order {order.Number} canceled (payment session expired). Stock restored.",
            ct);
    }

    private async Task HandleAccountUpdatedAsync(PaymentEvent evt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(evt.AccountId)) return;

        // Find the tenant's payment account globally — webhook has no tenant context.
        var accounts = await paymentAccountRepo.FindGlobalAsync(a => a.AccountId == evt.AccountId, ct);
        var account = accounts.FirstOrDefault();
        if (account is null) return;

        var wasComplete = account.CanAcceptPayments;
        account.SyncStatus(evt.ChargesEnabled, evt.DetailsSubmitted);
        paymentAccountRepo.Update(account);
        await paymentAccountRepo.SaveChangesAsync(ct);

        if (!wasComplete && account.CanAcceptPayments)
        {
            await auditService.LogAsync(
                "payments.account_ready",
                "TenantPaymentAccount",
                account.Id,
                $"Connected {account.Provider} account {account.AccountId} completed onboarding.",
                ct);
        }
    }

    private async Task EnqueueOrderEmailAsync(EmailTemplateType type, Order order, CancellationToken ct)
    {
        var customers = await customerRepo.FindGlobalAsync(c => c.Id == order.CustomerId, ct);
        var customer = customers.FirstOrDefault();
        if (customer is null) return;

        var tenant = await tenantRepo.GetByIdAsync(order.TenantId, ct);
        if (tenant is null) return;

        await OrderEmailDispatcher.EnqueueAsync(
            jobQueue,
            type,
            order,
            customer,
            tenant.Name,
            storeUrlBuilder.BuildUrl(tenant.Slug),
            ct);
    }
}
