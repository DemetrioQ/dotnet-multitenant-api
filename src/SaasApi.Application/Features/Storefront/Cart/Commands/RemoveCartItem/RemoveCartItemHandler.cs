using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Cart.Commands.RemoveCartItem;

public class RemoveCartItemHandler(
    IRepository<Domain.Entities.Cart> cartRepo,
    IRepository<CartItem> itemRepo,
    IRepository<Product> productRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<RemoveCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(RemoveCartItemCommand request, CancellationToken ct)
    {
        var carts = await cartRepo.FindAsync(c => c.CustomerId == currentCustomer.CustomerId, ct);
        var cart = carts.FirstOrDefault()
                   ?? throw new NotFoundException("Cart is empty.");

        var lines = await itemRepo.FindAsync(i => i.CartId == cart.Id && i.ProductId == request.ProductId, ct);
        var line = lines.FirstOrDefault()
                   ?? throw new NotFoundException("Item not in cart.");

        itemRepo.Remove(line);
        cart.Touch();
        cartRepo.Update(cart);
        await itemRepo.SaveChangesAsync(ct);

        return await CartReader.BuildAsync(cart, itemRepo, productRepo, ct);
    }
}
