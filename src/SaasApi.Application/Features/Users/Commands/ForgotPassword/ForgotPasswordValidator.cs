using FluentValidation;

namespace SaasApi.Application.Features.Users.Commands.ForgotPassword;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
