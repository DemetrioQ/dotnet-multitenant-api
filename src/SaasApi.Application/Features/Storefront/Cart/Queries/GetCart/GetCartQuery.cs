using MediatR;

namespace SaasApi.Application.Features.Storefront.Cart.Queries.GetCart;

public record GetCartQuery : IRequest<CartDto>;
