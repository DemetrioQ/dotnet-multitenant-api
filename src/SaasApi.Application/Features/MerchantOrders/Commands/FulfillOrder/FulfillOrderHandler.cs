using MediatR;
using SaasApi.Application.Common;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.MerchantOrders.Commands.FulfillOrder;

public class FulfillOrderHandler(
    IRepository<Order> orderRepo,
    IRepository<OrderItem> itemRepo,
    IRepository<Customer> customerRepo,
    IRepository<Tenant> tenantRepo,
    IStoreUrlBuilder storeUrlBuilder,
    IBackgroundJobQueue jobQueue,
    IAuditService auditService)
    : IRequestHandler<FulfillOrderCommand, MerchantOrderDto>
{
    public async Task<MerchantOrderDto> Handle(FulfillOrderCommand request, CancellationToken ct)
    {
        var matches = await orderRepo.FindAsync(o => o.Id == request.Id, ct);
        var order = matches.FirstOrDefault()
                    ?? throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Paid)
            throw new BadRequestException($"Only paid orders can be fulfilled. Current status: {order.Status.ToString().ToLowerInvariant()}.");

        order.MarkFulfilled();
        await orderRepo.SaveChangesAsync(ct);

        await auditService.LogAsync(
            "order.fulfilled",
            "Order",
            order.Id,
            $"Order {order.Number} marked fulfilled.",
            ct);

        var items = await itemRepo.FindAsync(i => i.OrderId == order.Id, ct);
        var customers = await customerRepo.FindAsync(c => c.Id == order.CustomerId, ct);
        var customer = customers.First();

        var tenant = await tenantRepo.GetByIdAsync(order.TenantId, ct);
        if (tenant is not null)
        {
            await OrderEmailDispatcher.EnqueueAsync(
                jobQueue,
                EmailTemplateType.OrderFulfilled,
                order,
                customer,
                tenant.Name,
                storeUrlBuilder.BuildUrl(tenant.Slug),
                ct);
        }

        return MerchantOrderDto.FromEntity(order, customer, items);
    }
}
