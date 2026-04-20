using MediatR;
using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Inventory;
using SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;
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
    IRepository<CustomerAddress> addressRepo,
    IRepository<TenantPaymentAccount> paymentAccountRepo,
    ICurrentTenantService currentTenant,
    ICurrentCustomerService currentCustomer,
    IPaymentService paymentService,
    IBackgroundJobQueue jobQueue,
    IConfiguration config)
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

        // Resolve shipping / billing from inline input or saved-address id. Shares the
        // same resolution logic as the non-payment POST /checkout path.
        var checkoutLikeCommand = new CheckoutCommand(
            request.ShippingAddress,
            request.BillingAddress,
            request.ShippingAddressId,
            request.BillingAddressId);
        var shipping = await CheckoutHandler.ResolveShippingAsync(checkoutLikeCommand, addressRepo, currentCustomer, ct);
        var billing = await CheckoutHandler.ResolveBillingAsync(checkoutLikeCommand, shipping, addressRepo, currentCustomer, ct);

        var platformFeePercent = config.GetValue<decimal?>("Payments:PlatformFeePercent") ?? 0.05m;

        var order = Order.Create(
            currentTenant.TenantId,
            currentCustomer.CustomerId,
            GenerateNumber(),
            subtotal,
            shipping,
            billing,
            platformFeePercent);
        await orderRepo.AddAsync(order, ct);

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
        }

        var customers = await customerRepo.FindAsync(c => c.Id == currentCustomer.CustomerId, ct);
        var customer = customers.First();

        const string currency = "USD";

        var paymentLines = cartItems.Select(ci => new PaymentSessionLineItem(
            productsById[ci.ProductId].Name,
            productsById[ci.ProductId].Price,
            ci.Quantity)).ToList();

        // Substitute {ORDER_ID} / {ORDER_NUMBER} placeholders before sending to the payment
        // provider. The frontend can't do this itself because it doesn't know the ids until
        // the response comes back — so it puts the placeholders in and we fill them in here.
        var successUrl = SubstituteOrderPlaceholders(request.SuccessUrl, order);
        var cancelUrl = SubstituteOrderPlaceholders(request.CancelUrl, order);

        // Look up the tenant's connected Stripe account (if any). When the Stripe provider is
        // active AND the account is complete, the charge is routed there with an application
        // fee — that's the multi-tenant Connect flow. When using the simulation provider, or
        // when Stripe hasn't been connected yet, ConnectedAccountId stays null and the charge
        // (or fake session) lands on the platform account.
        var paymentAccounts = await paymentAccountRepo.FindAsync(_ => true, ct);
        var paymentAccount = paymentAccounts.FirstOrDefault();

        // Stripe provider + no-or-incomplete account → block. Simulation has no such gate.
        if (paymentService.ProviderName == "stripe" && paymentAccount?.CanAcceptPayments != true)
            throw new BadRequestException(
                "This store isn't accepting online payments yet. The merchant needs to finish their Stripe Connect onboarding.");

        var connectedAccountId = paymentAccount?.Provider == paymentService.ProviderName
            ? paymentAccount.AccountId
            : null;

        var session = await paymentService.CreateSessionAsync(new CreatePaymentSessionRequest(
            currentTenant.TenantId,
            order.Id,
            order.Number,
            currency,
            paymentLines,
            customer.Email,
            successUrl,
            cancelUrl,
            connectedAccountId,
            platformFeePercent), ct);

        order.AttachPaymentSession(session.Provider, session.SessionId);
        // Don't call orderRepo.Update — the order is already in Added state; mutations are
        // tracked automatically. Calling Update flips it to Modified and breaks the insert.

        await orderRepo.SaveChangesAsync(ct);

        await LowStockAlerter.CheckAsync(jobQueue, config, stockChanges, ct);

        return new CreateCheckoutSessionResult(
            order.Id,
            order.Number,
            session.Provider,
            session.SessionId,
            session.PaymentUrl);
    }

    private static string GenerateNumber() =>
        "ORD-" + Guid.NewGuid().ToString("N").ToUpperInvariant().Substring(0, 10);

    private static string SubstituteOrderPlaceholders(string url, Order order) =>
        url
            .Replace("{ORDER_ID}", order.Id.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{ORDER_NUMBER}", order.Number, StringComparison.OrdinalIgnoreCase);
}
