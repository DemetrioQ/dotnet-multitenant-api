using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.MerchantOrders.Queries.GetMerchantOrderById;

public class GetMerchantOrderByIdHandler(
    IRepository<Order> orderRepo,
    IRepository<OrderItem> itemRepo,
    IRepository<Customer> customerRepo)
    : IRequestHandler<GetMerchantOrderByIdQuery, MerchantOrderDto>
{
    public async Task<MerchantOrderDto> Handle(GetMerchantOrderByIdQuery request, CancellationToken ct)
    {
        var matches = await orderRepo.FindAsync(o => o.Id == request.Id, ct);
        var order = matches.FirstOrDefault()
                    ?? throw new NotFoundException("Order not found.");

        var items = await itemRepo.FindAsync(i => i.OrderId == order.Id, ct);

        var customers = await customerRepo.FindAsync(c => c.Id == order.CustomerId, ct);
        var customer = customers.FirstOrDefault()
                       ?? throw new NotFoundException("Order customer not found.");

        return MerchantOrderDto.FromEntity(order, customer, items);
    }
}
