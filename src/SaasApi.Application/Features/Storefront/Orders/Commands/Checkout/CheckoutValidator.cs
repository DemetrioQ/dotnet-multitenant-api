using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

public class CheckoutValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutValidator()
    {
        // Must supply shipping via inline OR id, not both, not neither.
        RuleFor(x => x)
            .Must(x => (x.ShippingAddress != null) ^ (x.ShippingAddressId != null))
            .WithMessage("Provide exactly one of shippingAddress or shippingAddressId.");

        // Billing: if both are supplied, reject. Neither is fine (inherits shipping).
        RuleFor(x => x)
            .Must(x => !(x.BillingAddress != null && x.BillingAddressId != null))
            .WithMessage("Provide at most one of billingAddress or billingAddressId.");

        When(x => x.ShippingAddress != null, () =>
            RuleFor(x => x.ShippingAddress!).SetValidator(new CheckoutAddressValidator()));
        When(x => x.BillingAddress != null, () =>
            RuleFor(x => x.BillingAddress!).SetValidator(new CheckoutAddressValidator()));
    }
}

public class CheckoutAddressValidator : AbstractValidator<CheckoutAddressInput>
{
    public CheckoutAddressValidator()
    {
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().Length(2)
            .WithMessage("Country must be a 2-letter ISO code (e.g. 'US').");
    }
}
