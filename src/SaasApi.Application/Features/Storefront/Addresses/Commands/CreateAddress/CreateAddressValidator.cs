using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.CreateAddress;

public class CreateAddressValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.Label).MaximumLength(50);
        RuleFor(x => x.Address).NotNull().SetValidator(new AddressInputValidator());
    }
}
