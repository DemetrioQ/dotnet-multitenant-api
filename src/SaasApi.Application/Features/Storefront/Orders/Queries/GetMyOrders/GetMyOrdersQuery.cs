using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Storefront.Orders.Queries.GetMyOrders;

public record GetMyOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<OrderSummaryDto>>;
