using MediatR;
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
    IPaymentService paymentService,
    IAuditService auditService)
    : IRequestHandler<HandlePaymentWebhookCommand>
{
    public async Task Handle(HandlePaymentWebhookCommand request, CancellationToken ct)
    {
        // Invalid signatures throw UnauthorizedAccessException — controller maps that to 401.
        var evt = paymentService.ParseWebhook(request.Payload, request.SignatureHeader);

        if (evt.Kind == PaymentEventKind.Unknown) return;
        if (string.IsNullOrWhiteSpace(evt.SessionId)) return;

        // Webhook is not tenant-scoped — look up order globally by session id.
        var orders = await orderRepo.FindGlobalAsync(o => o.PaymentSessionId == evt.SessionId, ct);
        var order = orders.FirstOrDefault();
        if (order is null) return;

        switch (evt.Kind)
        {
            case PaymentEventKind.SessionCompleted:
                await HandleSessionCompletedAsync(order, ct);
                break;
            case PaymentEventKind.SessionExpired:
                await HandleSessionExpiredAsync(order, ct);
                break;
        }
    }

    private async Task HandleSessionCompletedAsync(Order order, CancellationToken ct)
    {
        if (order.Status != OrderStatus.Pending) return; // idempotent: already handled

        order.MarkPaid();
        orderRepo.Update(order);

        // Clear the customer's cart items.
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
    }

    private async Task HandleSessionExpiredAsync(Order order, CancellationToken ct)
    {
        if (order.Status != OrderStatus.Pending) return;

        // Restore stock for each line, then cancel.
        var items = await orderItemRepo.FindGlobalAsync(i => i.OrderId == order.Id, ct);
        foreach (var item in items)
        {
            var products = await productRepo.FindGlobalAsync(p => p.Id == item.ProductId, ct);
            var product = products.FirstOrDefault();
            if (product is null) continue;

            // Increment stock back — use Update (Product has no Increment, but Update allows us to set it).
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
}
