using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionHandler(
    IRepository<Domain.Entities.Cart> cartRepo,
    IRepository<CartItem> cartItemRepo,
    IRepository<Product> productRepo,
    IRepository<Order> orderRepo,
    IRepository<OrderItem> orderItemRepo,
    IRepository<Customer> customerRepo,
    IRepository<TenantSettings> settingsRepo,
    ICurrentTenantService currentTenant,
    ICurrentCustomerService currentCustomer,
    IPaymentService paymentService)
    : IRequestHandler<CreateCheckoutSessionCommand, CreateCheckoutSessionResult>
{
    public async Task<CreateCheckoutSessionResult> Handle(CreateCheckoutSessionCommand request, CancellationToken ct)
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

        var shipping = new Address(request.ShippingAddress.Line1, request.ShippingAddress.Line2,
            request.ShippingAddress.City, request.ShippingAddress.Region,
            request.ShippingAddress.PostalCode, request.ShippingAddress.Country);
        var b = request.BillingAddress ?? request.ShippingAddress;
        var billing = new Address(b.Line1, b.Line2, b.City, b.Region, b.PostalCode, b.Country);

        var order = Order.Create(
            currentTenant.TenantId,
            currentCustomer.CustomerId,
            GenerateNumber(),
            subtotal,
            shipping,
            billing);
        await orderRepo.AddAsync(order, ct);

        foreach (var ci in cartItems)
        {
            var p = productsById[ci.ProductId];
            p.DecrementStock(ci.Quantity);
            productRepo.Update(p);

            var oi = OrderItem.Create(currentTenant.TenantId, order.Id, p, ci.Quantity);
            await orderItemRepo.AddAsync(oi, ct);
        }

        var customers = await customerRepo.FindAsync(c => c.Id == currentCustomer.CustomerId, ct);
        var customer = customers.First();

        var settingsList = await settingsRepo.FindAsync(_ => true, ct);
        var currency = settingsList.FirstOrDefault()?.Currency ?? "USD";

        var paymentLines = cartItems.Select(ci => new PaymentSessionLineItem(
            productsById[ci.ProductId].Name,
            productsById[ci.ProductId].Price,
            ci.Quantity)).ToList();

        var session = await paymentService.CreateSessionAsync(new CreatePaymentSessionRequest(
            order.Id,
            order.Number,
            currency,
            paymentLines,
            customer.Email,
            request.SuccessUrl,
            request.CancelUrl), ct);

        order.AttachPaymentSession(session.Provider, session.SessionId);
        // Don't call orderRepo.Update — the order is already in Added state; mutations are
        // tracked automatically. Calling Update flips it to Modified and breaks the insert.

        await orderRepo.SaveChangesAsync(ct);

        return new CreateCheckoutSessionResult(
            order.Id,
            order.Number,
            session.Provider,
            session.SessionId,
            session.PaymentUrl);
    }

    private static string GenerateNumber() =>
        "ORD-" + Guid.NewGuid().ToString("N").ToUpperInvariant().Substring(0, 10);
}
