using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace SaasApi.Infrastructure.Services.Payments;

public class StripePaymentService : IPaymentService
{
    private readonly string _secretKey;
    private readonly string _webhookSecret;

    public string ProviderName => "stripe";

    public StripePaymentService(IConfiguration config)
    {
        _secretKey = config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");
        _webhookSecret = config["Stripe:WebhookSecret"]
            ?? throw new InvalidOperationException("Stripe:WebhookSecret is not configured.");
        StripeConfiguration.ApiKey = _secretKey;
    }

    public async Task<PaymentSession> CreateSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            CustomerEmail = request.CustomerEmail,
            ClientReferenceId = request.OrderId.ToString(),
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            LineItems = request.Items.Select(i => new SessionLineItemOptions
            {
                Quantity = i.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = request.Currency.ToLowerInvariant(),
                    UnitAmount = (long)(i.UnitAmount * 100m), // Stripe expects minor units
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = i.Name
                    }
                }
            }).ToList(),
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = request.OrderId.ToString(),
                ["orderNumber"] = request.OrderNumber
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        return new PaymentSession(ProviderName, session.Id, session.Url);
    }

    public PaymentEvent ParseWebhook(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            throw new UnauthorizedAccessException("Missing Stripe-Signature header.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _webhookSecret);
        }
        catch (StripeException)
        {
            throw new UnauthorizedAccessException("Invalid Stripe webhook signature.");
        }

        var kind = stripeEvent.Type switch
        {
            "checkout.session.completed" => PaymentEventKind.SessionCompleted,
            "checkout.session.expired" => PaymentEventKind.SessionExpired,
            _ => PaymentEventKind.Unknown
        };

        var sessionId = stripeEvent.Data.Object is Session s ? s.Id : string.Empty;
        return new PaymentEvent(stripeEvent.Id, sessionId, kind);
    }
}
