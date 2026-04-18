using MediatR;

namespace SaasApi.Application.Features.MerchantOrders.Commands.CancelOrder;

public record CancelOrderCommand(Guid Id) : IRequest<MerchantOrderDto>;
