using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterDynamicClient;

/// <summary>
/// RFC 7591 — OAuth 2.0 Dynamic Client Registration.
/// Anonymous endpoint. Creates a tenantless public client for PKCE flows
/// (e.g. Claude Code's hosted MCP). Tenant binds at the authorize step
/// from the user's JWT, not at registration.
/// </summary>
public record RegisterDynamicClientCommand(
    string? ClientName,
    IReadOnlyList<string>? RedirectUris,
    IReadOnlyList<string>? GrantTypes,
    IReadOnlyList<string>? ResponseTypes,
    string? TokenEndpointAuthMethod) : IRequest<RegisterDynamicClientResult>;

public record RegisterDynamicClientResult(
    string ClientId,
    long ClientIdIssuedAt,
    string? ClientName,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> ResponseTypes,
    string TokenEndpointAuthMethod,
    string Scope);
