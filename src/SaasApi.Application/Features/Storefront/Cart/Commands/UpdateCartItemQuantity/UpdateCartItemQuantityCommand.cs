using MediatR;

namespace SaasApi.Application.Features.Storefront.Cart.Commands.UpdateCartItemQuantity;

public record UpdateCartItemQuantityCommand(Guid ProductId, int Quantity) : IRequest<CartDto>;
