using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.MerchantOrders.Commands.CancelOrder;

public class CancelOrderHandler(
    IRepository<Order> orderRepo,
    IRepository<OrderItem> itemRepo,
    IRepository<Product> productRepo,
    IRepository<Customer> customerRepo,
    IAuditService auditService)
    : IRequestHandler<CancelOrderCommand, MerchantOrderDto>
{
    public async Task<MerchantOrderDto> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var matches = await orderRepo.FindAsync(o => o.Id == request.Id, ct);
        var order = matches.FirstOrDefault()
                    ?? throw new NotFoundException("Order not found.");

        if (order.Status is OrderStatus.Canceled or OrderStatus.Fulfilled or OrderStatus.Refunded)
            throw new BadRequestException($"Cannot cancel order with status {order.Status.ToString().ToLowerInvariant()}.");

        // Restore stock for every line.
        var items = await itemRepo.FindAsync(i => i.OrderId == order.Id, ct);
        foreach (var item in items)
        {
            var products = await productRepo.FindAsync(p => p.Id == item.ProductId, ct);
            var product = products.FirstOrDefault();
            if (product is null) continue;
            product.Update(product.Name, product.Description, product.Price, product.Stock + item.Quantity);
            productRepo.Update(product);
        }

        order.Cancel();
        await orderRepo.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "order.canceled",
            "Order",
            order.Id,
            $"Order {order.Number} canceled by merchant. Stock restored.",
            ct);

        var customers = await customerRepo.FindAsync(c => c.Id == order.CustomerId, ct);
        return MerchantOrderDto.FromEntity(order, customers.First(), items);
    }
}
