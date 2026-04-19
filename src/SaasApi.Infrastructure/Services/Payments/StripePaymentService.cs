using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace SaasApi.Infrastructure.Services.Payments;

public class StripePaymentService(IConfiguration config) : IPaymentService
{
    public string ProviderName => "stripe";

    private string SecretKey =>
        config["Stripe:SecretKey"]
        ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");

    private string WebhookSecret =>
        config["Stripe:WebhookSecret"]
        ?? throw new InvalidOperationException("Stripe:WebhookSecret is not configured.");

    public async Task<PaymentSession> CreateSessionAsync(CreatePaymentSessionRequest request, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = SecretKey;

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
                    UnitAmount = (long)(i.UnitAmount * 100m),
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = i.Name }
                }
            }).ToList(),
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = request.TenantId.ToString(),
                ["orderId"] = request.OrderId.ToString(),
                ["orderNumber"] = request.OrderNumber
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // If the tenant has a connected Stripe account, route the charge there and
        // take the platform cut as an application fee. Otherwise the charge hits the
        // platform's own Stripe account (fallback / early-demo path).
        RequestOptions? reqOpts = null;
        if (!string.IsNullOrWhiteSpace(request.ConnectedAccountId))
        {
            var subtotal = request.Items.Sum(i => i.UnitAmount * i.Quantity);
            var feeCents = (long)Math.Round(subtotal * request.PlatformFeePercent * 100m);
            if (feeCents > 0)
            {
                options.PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = feeCents
                };
            }
            reqOpts = new RequestOptions { StripeAccount = request.ConnectedAccountId };
        }

        var service = new SessionService();
        var session = reqOpts is null
            ? await service.CreateAsync(options, cancellationToken: ct)
            : await service.CreateAsync(options, reqOpts, ct);

        return new PaymentSession(ProviderName, session.Id, session.Url);
    }

    public PaymentEvent ParseWebhook(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            throw new UnauthorizedAccessException("Missing Stripe-Signature header.");

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, WebhookSecret);
        }
        catch (StripeException)
        {
            throw new UnauthorizedAccessException("Invalid Stripe webhook signature.");
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            case "checkout.session.expired":
                {
                    var kind = stripeEvent.Type == "checkout.session.completed"
                        ? PaymentEventKind.SessionCompleted
                        : PaymentEventKind.SessionExpired;
                    var sessionId = stripeEvent.Data.Object is Session s ? s.Id : string.Empty;
                    return new PaymentEvent(stripeEvent.Id, sessionId, kind);
                }
            case "account.updated":
                {
                    var account = stripeEvent.Data.Object as Account;
                    return new PaymentEvent(
                        stripeEvent.Id, "", PaymentEventKind.AccountUpdated,
                        AccountId: account?.Id ?? "",
                        ChargesEnabled: account?.ChargesEnabled ?? false,
                        DetailsSubmitted: account?.DetailsSubmitted ?? false);
                }
            default:
                return new PaymentEvent(stripeEvent.Id, "", PaymentEventKind.Unknown);
        }
    }

    public async Task<CreateConnectAccountResult> CreateConnectAccountAsync(
        Guid tenantId, string tenantName, string tenantEmail, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var options = new AccountCreateOptions
        {
            Type = "express",
            Email = tenantEmail,
            BusinessProfile = new AccountBusinessProfileOptions { Name = tenantName },
            Metadata = new Dictionary<string, string> { ["tenantId"] = tenantId.ToString() },
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
            }
        };

        var service = new AccountService();
        var account = await service.CreateAsync(options, cancellationToken: ct);
        return new CreateConnectAccountResult(ProviderName, account.Id);
    }

    public async Task<ConnectOnboardingLink> CreateOnboardingLinkAsync(
        string accountId, string refreshUrl, string returnUrl, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var options = new AccountLinkCreateOptions
        {
            Account = accountId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding"
        };

        var service = new AccountLinkService();
        var link = await service.CreateAsync(options, cancellationToken: ct);
        return new ConnectOnboardingLink(link.Url, link.ExpiresAt);
    }

    public async Task<ConnectAccountStatusInfo> RefreshAccountStatusAsync(string accountId, CancellationToken ct)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var service = new AccountService();
        var account = await service.GetAsync(accountId, cancellationToken: ct);
        return new ConnectAccountStatusInfo(account.ChargesEnabled, account.DetailsSubmitted);
    }
}
