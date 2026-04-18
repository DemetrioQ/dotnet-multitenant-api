using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Cart.Commands.ClearCart;

public class ClearCartHandler(
    IRepository<Domain.Entities.Cart> cartRepo,
    IRepository<CartItem> itemRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken ct)
    {
        var carts = await cartRepo.FindAsync(c => c.CustomerId == currentCustomer.CustomerId, ct);
        var cart = carts.FirstOrDefault();
        if (cart is null) return;

        var lines = await itemRepo.FindAsync(i => i.CartId == cart.Id, ct);
        if (lines.Count == 0) return;

        foreach (var line in lines)
            itemRepo.Remove(line);

        cart.Touch();
        cartRepo.Update(cart);
        await itemRepo.SaveChangesAsync(ct);
    }
}
