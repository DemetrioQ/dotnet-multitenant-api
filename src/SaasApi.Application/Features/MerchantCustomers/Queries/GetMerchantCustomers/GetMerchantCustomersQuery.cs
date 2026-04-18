using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.MerchantCustomers.Queries.GetMerchantCustomers;

public record GetMerchantCustomersQuery(int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<MerchantCustomerSummaryDto>>;
