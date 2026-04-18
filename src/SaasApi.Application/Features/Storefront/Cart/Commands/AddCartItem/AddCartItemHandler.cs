using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Cart.Commands.AddCartItem;

public class AddCartItemHandler(
    IRepository<Domain.Entities.Cart> cartRepo,
    IRepository<CartItem> itemRepo,
    IRepository<Product> productRepo,
    ICurrentCustomerService currentCustomer,
    ICurrentTenantService currentTenant)
    : IRequestHandler<AddCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(AddCartItemCommand request, CancellationToken ct)
    {
        var product = await productRepo.GetByIdAsync(request.ProductId, ct);
        if (product is null || !product.IsActive)
            throw new NotFoundException("Product not found.");

        var carts = await cartRepo.FindAsync(c => c.CustomerId == currentCustomer.CustomerId, ct);
        var cart = carts.FirstOrDefault();

        if (cart is null)
        {
            cart = Domain.Entities.Cart.Create(currentTenant.TenantId, currentCustomer.CustomerId);
            await cartRepo.AddAsync(cart, ct);
            await cartRepo.SaveChangesAsync(ct);
        }

        var existingLines = await itemRepo.FindAsync(i => i.CartId == cart.Id && i.ProductId == product.Id, ct);
        var existingLine = existingLines.FirstOrDefault();

        var newQuantity = (existingLine?.Quantity ?? 0) + request.Quantity;
        if (newQuantity > product.Stock)
            throw new BadRequestException($"Only {product.Stock} unit(s) of '{product.Name}' are available.");

        if (existingLine is null)
        {
            var line = CartItem.Create(currentTenant.TenantId, cart.Id, product.Id, request.Quantity);
            await itemRepo.AddAsync(line, ct);
        }
        else
        {
            existingLine.Increment(request.Quantity);
            itemRepo.Update(existingLine);
        }

        cart.Touch();
        cartRepo.Update(cart);
        await itemRepo.SaveChangesAsync(ct);

        return await CartReader.BuildAsync(cart, itemRepo, productRepo, ct);
    }
}
