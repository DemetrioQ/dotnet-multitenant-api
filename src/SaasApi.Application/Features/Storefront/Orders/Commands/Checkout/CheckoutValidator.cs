using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;

public class CheckoutValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.ShippingAddress).NotNull().SetValidator(new CheckoutAddressValidator());
        When(x => x.BillingAddress != null, () =>
        {
            RuleFor(x => x.BillingAddress!).SetValidator(new CheckoutAddressValidator());
        });
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
