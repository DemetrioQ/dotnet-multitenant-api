using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Commands.RevokeOAuthClient;

public class RevokeOAuthClientHandler(IRepository<OAuthClient> clientRepo)
    : IRequestHandler<RevokeOAuthClientCommand>
{
    public async Task Handle(RevokeOAuthClientCommand request, CancellationToken ct)
    {
        var client = await clientRepo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"OAuth client {request.Id} not found.");

        if (client.IsRevoked) return;

        client.Revoke();
        clientRepo.Update(client);
        await clientRepo.SaveChangesAsync(ct);
    }
}
