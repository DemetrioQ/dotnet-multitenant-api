using FluentValidation;
using SaasApi.Domain.Common;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;

public class RegisterOAuthClientValidator : AbstractValidator<RegisterOAuthClientCommand>
{
    private static readonly string[] AllowedTypes = ["confidential", "public"];

    public RegisterOAuthClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ClientType)
            .Must(t => AllowedTypes.Contains(t, StringComparer.Ordinal))
            .WithMessage($"ClientType must be one of: {string.Join(", ", AllowedTypes)}");

        RuleFor(x => x.Scopes)
            .NotNull().WithMessage("At least one scope is required.")
            .Must(s => s != null && s.Count > 0).WithMessage("At least one scope is required.")
            .Must(s => s == null || s.All(OAuthScopes.IsValid))
            .WithMessage($"Unknown scope. Valid scopes: {string.Join(", ", OAuthScopes.All)}");

        // Redirect URIs are required for public clients (PKCE flow needs them).
        // Each must parse as an absolute URI to prevent open-redirect bugs.
        When(x => x.ClientType == "public", () =>
        {
            RuleFor(x => x.RedirectUris)
                .NotNull().WithMessage("Public clients require at least one redirect URI.")
                .Must(uris => uris != null && uris.Count > 0)
                .WithMessage("Public clients require at least one redirect URI.")
                .Must(uris => uris == null || uris.All(IsValidAbsoluteUri))
                .WithMessage("All redirect URIs must be absolute URLs (e.g. http://127.0.0.1:54321/callback).");
        });
    }

    private static bool IsValidAbsoluteUri(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out _);
}
