using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.MerchantCustomers.Queries.GetMerchantCustomers;

public class GetMerchantCustomersHandler(
    IRepository<Customer> customerRepo,
    IRepository<Order> orderRepo)
    : IRequestHandler<GetMerchantCustomersQuery, PagedResult<MerchantCustomerSummaryDto>>
{
    public async Task<PagedResult<MerchantCustomerSummaryDto>> Handle(GetMerchantCustomersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var all = await customerRepo.GetAllAsync(ct);
        var ordered = all.OrderByDescending(c => c.CreatedAt).ToList();
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        if (paged.Count == 0)
            return new PagedResult<MerchantCustomerSummaryDto>(
                Array.Empty<MerchantCustomerSummaryDto>(), ordered.Count, page, pageSize);

        var customerIds = paged.Select(c => c.Id).ToHashSet();
        var orders = await orderRepo.FindAsync(o => customerIds.Contains(o.CustomerId), ct);

        var ordersByCustomer = orders.GroupBy(o => o.CustomerId).ToDictionary(g => g.Key, g => g.ToList());

        var dtos = paged.Select(c =>
        {
            var customerOrders = ordersByCustomer.GetValueOrDefault(c.Id, new List<Order>());
            var paid = customerOrders.Where(o => o.Status is OrderStatus.Paid or OrderStatus.Fulfilled);
            return new MerchantCustomerSummaryDto(
                c.Id, c.Email, c.FirstName, c.LastName,
                c.IsActive, c.IsEmailVerified,
                customerOrders.Count,
                paid.Sum(o => o.Total),
                c.CreatedAt);
        }).ToList();

        return new PagedResult<MerchantCustomerSummaryDto>(dtos, ordered.Count, page, pageSize);
    }
}
