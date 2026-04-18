using MediatR;

namespace SaasApi.Application.Features.Storefront.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;
