using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;

/// <summary>
/// Register an OAuth client. ClientType="confidential" returns a one-time secret
/// (used by client_credentials grant). ClientType="public" returns no secret —
/// it must use authorization_code + PKCE and provide RedirectUris.
/// </summary>
public record RegisterOAuthClientCommand(
    string Name,
    IReadOnlyList<string> Scopes,
    string ClientType = "confidential",
    IReadOnlyList<string>? RedirectUris = null) : IRequest<RegisterOAuthClientResult>;

public record RegisterOAuthClientResult(
    string ClientId,
    string? ClientSecret,
    string Name,
    string ClientType,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> RedirectUris);
