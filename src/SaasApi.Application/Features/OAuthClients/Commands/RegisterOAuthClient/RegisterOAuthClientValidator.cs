using FluentValidation;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;

public class RegisterOAuthClientValidator : AbstractValidator<RegisterOAuthClientCommand>
{
    public RegisterOAuthClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
