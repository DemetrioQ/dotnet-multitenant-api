using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Queries.GetAuthorizeRequestInfo;

public record GetAuthorizeRequestInfoQuery(
    string ClientId,
    string RedirectUri,
    string Scope) : IRequest<AuthorizeRequestInfoDto>;

public record AuthorizeRequestInfoDto(
    string ClientId,
    string ClientName,
    IReadOnlyList<string> RequestedScopes,
    IReadOnlyList<string> GrantableScopes,
    string RedirectUri);
