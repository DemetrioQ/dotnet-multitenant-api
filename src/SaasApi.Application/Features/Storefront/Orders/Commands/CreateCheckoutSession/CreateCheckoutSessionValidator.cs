using FluentValidation;
using SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.ShippingAddress != null) ^ (x.ShippingAddressId != null))
            .WithMessage("Provide exactly one of shippingAddress or shippingAddressId.");

        RuleFor(x => x)
            .Must(x => !(x.BillingAddress != null && x.BillingAddressId != null))
            .WithMessage("Provide at most one of billingAddress or billingAddressId.");

        When(x => x.ShippingAddress != null, () =>
            RuleFor(x => x.ShippingAddress!).SetValidator(new CheckoutAddressValidator()));
        When(x => x.BillingAddress != null, () =>
            RuleFor(x => x.BillingAddress!).SetValidator(new CheckoutAddressValidator()));

        RuleFor(x => x.SuccessUrl).NotEmpty().Must(BeAbsoluteUrl)
            .WithMessage("SuccessUrl must be a valid absolute URL.");
        RuleFor(x => x.CancelUrl).NotEmpty().Must(BeAbsoluteUrl)
            .WithMessage("CancelUrl must be a valid absolute URL.");
    }

    private static bool BeAbsoluteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
