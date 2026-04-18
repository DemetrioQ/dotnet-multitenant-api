using MediatR;

namespace SaasApi.Application.Features.MerchantOrders.Commands.FulfillOrder;

public record FulfillOrderCommand(Guid Id) : IRequest<MerchantOrderDto>;
