using MediatR;
using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Inventory;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

public class CheckoutHandler(
    IRepository<Domain.Entities.Cart> cartRepo,
    IRepository<CartItem> cartItemRepo,
    IRepository<Product> productRepo,
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo,
    ICurrentTenantService currentTenant,
    ICurrentCustomerService currentCustomer,
    IBackgroundJobQueue jobQueue,
    IConfiguration config)
    : IRequestHandler<CheckoutCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CheckoutCommand request, CancellationToken ct)
    {
        var carts = await cartRepo.FindAsync(c => c.CustomerId == currentCustomer.CustomerId, ct);
        var cart = carts.FirstOrDefault()
                   ?? throw new BadRequestException("Cart is empty.");

        var cartItems = await cartItemRepo.FindAsync(i => i.CartId == cart.Id, ct);
        if (cartItems.Count == 0)
            throw new BadRequestException("Cart is empty.");

        var productIds = cartItems.Select(i => i.ProductId).ToHashSet();
        var products = await productRepo.FindAsync(p => productIds.Contains(p.Id), ct);
        var productsById = products.ToDictionary(p => p.Id);

        foreach (var ci in cartItems)
        {
            if (!productsById.TryGetValue(ci.ProductId, out var p) || !p.IsActive)
                throw new BadRequestException("A product in your cart is no longer available.");
            if (p.Stock < ci.Quantity)
                throw new BadRequestException($"Only {p.Stock} unit(s) of '{p.Name}' are available.");
        }

        var subtotal = cartItems.Sum(ci => productsById[ci.ProductId].Price * ci.Quantity);

        var shipping = Map(request.ShippingAddress);
        var billing = Map(request.BillingAddress ?? request.ShippingAddress);

        var order = Order.Create(
            currentTenant.TenantId,
            currentCustomer.CustomerId,
            GenerateNumber(),
            subtotal,
            shipping,
            billing);
        await orderRepo.AddAsync(order, ct);

        var orderItems = new List<OrderItem>(cartItems.Count);
        var stockChanges = new List<(Product, int)>(cartItems.Count);
        foreach (var ci in cartItems)
        {
            var p = productsById[ci.ProductId];
            var previousStock = p.Stock;
            p.DecrementStock(ci.Quantity);
            productRepo.Update(p);
            stockChanges.Add((p, previousStock));

            var oi = OrderItem.Create(currentTenant.TenantId, order.Id, p, ci.Quantity);
            await orderItemRepo.AddAsync(oi, ct);
            orderItems.Add(oi);

            cartItemRepo.Remove(ci);
        }

        await orderRepo.SaveChangesAsync(ct);

        await LowStockAlerter.CheckAsync(jobQueue, config, stockChanges, ct);

        return OrderDto.FromEntity(order, orderItems);
    }

    private static Address Map(CheckoutAddressInput a) =>
        new(a.Line1, a.Line2, a.City, a.Region, a.PostalCode, a.Country);

    private static string GenerateNumber() =>
        "ORD-" + Guid.NewGuid().ToString("N").ToUpperInvariant().Substring(0, 10);
}
