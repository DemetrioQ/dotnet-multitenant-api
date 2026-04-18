using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Cart.Queries.GetCart;

public class GetCartHandler(
    IRepository<Domain.Entities.Cart> cartRepo,
    IRepository<CartItem> itemRepo,
    IRepository<Product> productRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken ct)
    {
        var carts = await cartRepo.FindAsync(c => c.CustomerId == currentCustomer.CustomerId, ct);
        var cart = carts.FirstOrDefault();

        if (cart is null)
            return new CartDto(Guid.Empty, Array.Empty<CartLineDto>(), 0m, 0);

        return await CartReader.BuildAsync(cart, itemRepo, productRepo, ct);
    }
}
