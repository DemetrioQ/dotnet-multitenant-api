using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ResetCustomerPassword;

public class ResetCustomerPasswordValidator : AbstractValidator<ResetCustomerPasswordCommand>
{
    public ResetCustomerPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
