using MediatR;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.OAuthClients.Queries.GetOAuthClients;

public record GetOAuthClientsQuery : IRequest<IReadOnlyList<OAuthClientDto>>;

public record OAuthClientDto(
    Guid Id,
    string ClientId,
    string Name,
    string ClientType,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> RedirectUris,
    bool IsRevoked,
    DateTime CreatedAt,
    DateTime? LastUsedAt)
{
    public static OAuthClientDto FromEntity(OAuthClient entity) =>
        new(entity.Id,
            entity.ClientId,
            entity.Name,
            entity.ClientType.ToString().ToLowerInvariant(),
            entity.GetScopes(),
            entity.GetRedirectUris(),
            entity.IsRevoked,
            entity.CreatedAt,
            entity.LastUsedAt);
}
