using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Commands.IssueClientToken;

public class IssueClientTokenHandler(
    IAppDbContext db,
    IRepository<OAuthClient> clientRepo,
    IPasswordHasher hasher,
    IJwtTokenService jwt) : IRequestHandler<IssueClientTokenCommand, IssueClientTokenResult>
{
    private const int TokenLifetimeSeconds = 3600;

    public async Task<IssueClientTokenResult> Handle(IssueClientTokenCommand request, CancellationToken ct)
    {
        // Anonymous endpoint: bypass tenant query filter to find the client
        // across all tenants, then issue a token scoped to its TenantId.
        var client = await db.OAuthClients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ClientId == request.ClientId, ct);

        if (client is null || client.IsRevoked || !hasher.Verify(request.ClientSecret, client.ClientSecretHash))
            throw new UnauthorizedAccessException("invalid_client");

        client.MarkUsed();
        await clientRepo.SaveChangesAsync(ct);

        var token = jwt.GenerateToken(client);
        return new IssueClientTokenResult(token, TokenLifetimeSeconds);
    }
}
