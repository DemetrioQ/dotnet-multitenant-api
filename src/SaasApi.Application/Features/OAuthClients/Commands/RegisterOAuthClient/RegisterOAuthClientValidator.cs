using FluentValidation;
using SaasApi.Domain.Common;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;

public class RegisterOAuthClientValidator : AbstractValidator<RegisterOAuthClientCommand>
{
    public RegisterOAuthClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Scopes)
            .NotNull().WithMessage("At least one scope is required.")
            .Must(s => s != null && s.Count > 0).WithMessage("At least one scope is required.")
            .Must(s => s == null || s.All(OAuthScopes.IsValid))
            .WithMessage($"Unknown scope. Valid scopes: {string.Join(", ", OAuthScopes.All)}");
    }
}
