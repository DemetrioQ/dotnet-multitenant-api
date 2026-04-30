using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.MerchantOrders.Queries.GetMerchantOrders;

public record GetMerchantOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<PagedResult<MerchantOrderSummaryDto>>;
