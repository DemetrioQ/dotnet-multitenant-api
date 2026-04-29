using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Queries.GetOAuthClients;

public class GetOAuthClientsHandler(IRepository<OAuthClient> clientRepo)
    : IRequestHandler<GetOAuthClientsQuery, IReadOnlyList<OAuthClientDto>>
{
    public async Task<IReadOnlyList<OAuthClientDto>> Handle(GetOAuthClientsQuery request, CancellationToken ct)
    {
        var clients = await clientRepo.GetAllAsync(ct);
        return clients
            .OrderByDescending(c => c.CreatedAt)
            .Select(OAuthClientDto.FromEntity)
            .ToList();
    }
}
