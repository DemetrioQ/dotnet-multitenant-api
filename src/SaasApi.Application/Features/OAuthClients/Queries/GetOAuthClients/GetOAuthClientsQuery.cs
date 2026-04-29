using MediatR;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.OAuthClients.Queries.GetOAuthClients;

public record GetOAuthClientsQuery : IRequest<IReadOnlyList<OAuthClientDto>>;

public record OAuthClientDto(
    Guid Id,
    string ClientId,
    string Name,
    bool IsRevoked,
    DateTime CreatedAt,
    DateTime? LastUsedAt)
{
    public static OAuthClientDto FromEntity(OAuthClient entity) =>
        new(entity.Id, entity.ClientId, entity.Name, entity.IsRevoked, entity.CreatedAt, entity.LastUsedAt);
}
