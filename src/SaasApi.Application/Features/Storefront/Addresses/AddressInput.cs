using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Addresses;

public record AddressInput(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country);

public class AddressInputValidator : AbstractValidator<AddressInput>
{
    public AddressInputValidator()
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
