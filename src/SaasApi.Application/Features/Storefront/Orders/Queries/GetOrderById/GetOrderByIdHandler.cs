using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Orders.Queries.GetOrderById;

public class GetOrderByIdHandler(
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var matches = await orderRepo.FindAsync(
            o => o.Id == request.Id && o.CustomerId == currentCustomer.CustomerId, ct);
        var order = matches.FirstOrDefault()
                    ?? throw new NotFoundException("Order not found.");

        var items = await orderItemRepo.FindAsync(i => i.OrderId == order.Id, ct);
        return OrderDto.FromEntity(order, items);
    }
}
