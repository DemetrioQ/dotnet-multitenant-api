using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.MerchantOrders.Queries.GetMerchantOrders;

public class GetMerchantOrdersHandler(IAppDbContext db)
    : IRequestHandler<GetMerchantOrdersQuery, PagedResult<MerchantOrderSummaryDto>>
{
    public async Task<PagedResult<MerchantOrderSummaryDto>> Handle(GetMerchantOrdersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = db.Orders.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }
        if (request.From is { } from)
            query = query.Where(o => o.CreatedAt >= from);
        if (request.To is { } to)
            query = query.Where(o => o.CreatedAt < to);

        var total = await query.CountAsync(ct);
        if (total == 0)
            return new PagedResult<MerchantOrderSummaryDto>(Array.Empty<MerchantOrderSummaryDto>(), 0, page, pageSize);

        // SQL-side page + summary shape. Join customer inline; compute itemCount via
        // correlated subquery (EF translates the Sum into the SELECT list).
        var summaries = await (from o in query
                               orderby o.CreatedAt descending
                               join c in db.Customers on o.CustomerId equals c.Id
                               select new MerchantOrderSummaryDto(
                                   o.Id,
                                   o.Number,
                                   o.Status.ToString().ToLower(),
                                   c.Id,
                                   c.Email,
                                   c.FirstName + " " + c.LastName,
                                   o.Total,
                                   db.OrderItems.Where(i => i.OrderId == o.Id).Sum(i => (int?)i.Quantity) ?? 0,
                                   o.CreatedAt))
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync(ct);

        return new PagedResult<MerchantOrderSummaryDto>(summaries, total, page, pageSize);
    }
}
