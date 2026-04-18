using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.MerchantOrders.Queries.GetMerchantOrders;

public class GetMerchantOrdersHandler(
    IRepository<Order> orderRepo,
    IRepository<OrderItem> itemRepo,
    IRepository<Customer> customerRepo)
    : IRequestHandler<GetMerchantOrdersQuery, PagedResult<MerchantOrderSummaryDto>>
{
    public async Task<PagedResult<MerchantOrderSummaryDto>> Handle(GetMerchantOrdersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        OrderStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var all = statusFilter.HasValue
            ? await orderRepo.FindAsync(o => o.Status == statusFilter.Value, ct)
            : (IReadOnlyList<Order>)await orderRepo.GetAllAsync(ct);

        var ordered = all.OrderByDescending(o => o.CreatedAt).ToList();
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        if (paged.Count == 0)
            return new PagedResult<MerchantOrderSummaryDto>(Array.Empty<MerchantOrderSummaryDto>(), ordered.Count, page, pageSize);

        var orderIds = paged.Select(o => o.Id).ToHashSet();
        var items = await itemRepo.FindAsync(i => orderIds.Contains(i.OrderId), ct);
        var countsByOrder = items.GroupBy(i => i.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var customerIds = paged.Select(o => o.CustomerId).ToHashSet();
        var customers = await customerRepo.FindAsync(c => customerIds.Contains(c.Id), ct);
        var customersById = customers.ToDictionary(c => c.Id);

        var dtos = paged
            .Where(o => customersById.ContainsKey(o.CustomerId))
            .Select(o => MerchantOrderSummaryDto.FromEntity(
                o,
                customersById[o.CustomerId],
                countsByOrder.GetValueOrDefault(o.Id, 0)))
            .ToList();

        return new PagedResult<MerchantOrderSummaryDto>(dtos, ordered.Count, page, pageSize);
    }
}
