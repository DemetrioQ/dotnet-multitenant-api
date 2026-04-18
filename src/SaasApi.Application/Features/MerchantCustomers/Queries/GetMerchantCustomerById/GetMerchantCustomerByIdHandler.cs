using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.MerchantCustomers.Queries.GetMerchantCustomerById;

public class GetMerchantCustomerByIdHandler(
    IRepository<Customer> customerRepo,
    IRepository<Order> orderRepo)
    : IRequestHandler<GetMerchantCustomerByIdQuery, MerchantCustomerDto>
{
    public async Task<MerchantCustomerDto> Handle(GetMerchantCustomerByIdQuery request, CancellationToken ct)
    {
        var matches = await customerRepo.FindAsync(c => c.Id == request.Id, ct);
        var customer = matches.FirstOrDefault()
                       ?? throw new NotFoundException("Customer not found.");

        var orders = await orderRepo.FindAsync(o => o.CustomerId == customer.Id, ct);
        var orderDtos = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new MerchantCustomerOrderDto(
                o.Id, o.Number, o.Status.ToString().ToLowerInvariant(), o.Total, o.CreatedAt))
            .ToList();

        var paid = orders.Where(o => o.Status is OrderStatus.Paid or OrderStatus.Fulfilled);

        return new MerchantCustomerDto(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.IsActive,
            customer.IsEmailVerified,
            orders.Count,
            paid.Sum(o => o.Total),
            customer.CreatedAt,
            orderDtos);
    }
}
