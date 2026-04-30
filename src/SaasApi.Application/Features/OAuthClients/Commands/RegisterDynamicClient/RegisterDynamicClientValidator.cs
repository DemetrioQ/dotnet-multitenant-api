using FluentValidation;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterDynamicClient;

public class RegisterDynamicClientValidator : AbstractValidator<RegisterDynamicClientCommand>
{
    public RegisterDynamicClientValidator()
    {
        RuleFor(x => x.RedirectUris)
            .NotNull().WithMessage("redirect_uris is required.")
            .Must(uris => uris != null && uris.Count > 0)
            .WithMessage("redirect_uris must contain at least one URI.")
            .Must(uris => uris == null || uris.All(IsValidAbsoluteUri))
            .WithMessage("All redirect_uris must be absolute URLs.");

        // We only DCR public clients (no client_secret). Confidential client
        // registration stays admin-only on the dashboard.
        RuleFor(x => x.TokenEndpointAuthMethod)
            .Must(m => m is null or "none")
            .WithMessage("Only token_endpoint_auth_method=\"none\" is supported via dynamic registration.");

        RuleFor(x => x.GrantTypes)
            .Must(g => g == null || g.Count == 0 || g.Contains("authorization_code"))
            .WithMessage("grant_types must include \"authorization_code\".");

        RuleFor(x => x.ResponseTypes)
            .Must(r => r == null || r.Count == 0 || r.Contains("code"))
            .WithMessage("response_types must include \"code\".");

        RuleFor(x => x.ClientName)
            .MaximumLength(100);
    }

    private static bool IsValidAbsoluteUri(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out _);
}
