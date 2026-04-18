using MediatR;

namespace SaasApi.Application.Features.Storefront.Cart.Commands.RemoveCartItem;

public record RemoveCartItemCommand(Guid ProductId) : IRequest<CartDto>;
