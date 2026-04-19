using FluentValidation;

namespace SaasApi.Application.Features.Payments.Connect.Commands.StartConnectOnboarding;

public class StartConnectOnboardingValidator : AbstractValidator<StartConnectOnboardingCommand>
{
    public StartConnectOnboardingValidator()
    {
        RuleFor(x => x.RefreshUrl).NotEmpty().Must(BeAbsolute)
            .WithMessage("RefreshUrl must be an absolute http/https URL.");
        RuleFor(x => x.ReturnUrl).NotEmpty().Must(BeAbsolute)
            .WithMessage("ReturnUrl must be an absolute http/https URL.");
    }

    private static bool BeAbsolute(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
