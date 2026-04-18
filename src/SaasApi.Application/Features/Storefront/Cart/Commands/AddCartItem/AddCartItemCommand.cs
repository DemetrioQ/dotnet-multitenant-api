using MediatR;

namespace SaasApi.Application.Features.Storefront.Cart.Commands.AddCartItem;

public record AddCartItemCommand(Guid ProductId, int Quantity) : IRequest<CartDto>;
