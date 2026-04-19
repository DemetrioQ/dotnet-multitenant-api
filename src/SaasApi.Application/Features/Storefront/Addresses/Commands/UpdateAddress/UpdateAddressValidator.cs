using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.UpdateAddress;

public class UpdateAddressValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).MaximumLength(50);
        RuleFor(x => x.Address).NotNull().SetValidator(new AddressInputValidator());
    }
}
