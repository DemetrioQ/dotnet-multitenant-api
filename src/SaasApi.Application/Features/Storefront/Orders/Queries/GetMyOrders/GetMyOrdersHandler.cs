using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Orders.Queries.GetMyOrders;

public class GetMyOrdersHandler(
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<GetMyOrdersQuery, PagedResult<OrderSummaryDto>>
{
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetMyOrdersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var all = await orderRepo.FindAsync(o => o.CustomerId == currentCustomer.CustomerId, ct);
        var ordered = all.OrderByDescending(o => o.CreatedAt).ToList();

        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        if (paged.Count == 0)
            return new PagedResult<OrderSummaryDto>(Array.Empty<OrderSummaryDto>(), ordered.Count, page, pageSize);

        var orderIds = paged.Select(o => o.Id).ToHashSet();
        var items = await orderItemRepo.FindAsync(i => orderIds.Contains(i.OrderId), ct);
        var countsByOrder = items.GroupBy(i => i.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var dtos = paged
            .Select(o => OrderSummaryDto.FromEntity(o, countsByOrder.GetValueOrDefault(o.Id, 0)))
            .ToList();

        return new PagedResult<OrderSummaryDto>(dtos, ordered.Count, page, pageSize);
    }
}
