using FluentValidation;
using SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.CreateCheckoutSession;

public class CreateCheckoutSessionValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionValidator()
    {
        RuleFor(x => x.ShippingAddress).NotNull().SetValidator(new CheckoutAddressValidator());
        When(x => x.BillingAddress != null, () =>
        {
            RuleFor(x => x.BillingAddress!).SetValidator(new CheckoutAddressValidator());
        });
        RuleFor(x => x.SuccessUrl).NotEmpty().Must(BeAbsoluteUrl)
            .WithMessage("SuccessUrl must be a valid absolute URL.");
        RuleFor(x => x.CancelUrl).NotEmpty().Must(BeAbsoluteUrl)
            .WithMessage("CancelUrl must be a valid absolute URL.");
    }

    private static bool BeAbsoluteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
