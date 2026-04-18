using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.LoginCustomer;

public class LoginCustomerValidator : AbstractValidator<LoginCustomerCommand>
{
    public LoginCustomerValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
