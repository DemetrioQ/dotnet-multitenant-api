using MediatR;

namespace SaasApi.Application.Features.MerchantOrders.Queries.GetMerchantOrderById;

public record GetMerchantOrderByIdQuery(Guid Id) : IRequest<MerchantOrderDto>;
