using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ForgotCustomerPassword;

public class ForgotCustomerPasswordValidator : AbstractValidator<ForgotCustomerPasswordCommand>
{
    public ForgotCustomerPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
