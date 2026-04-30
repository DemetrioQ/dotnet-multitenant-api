using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.OAuthClients.Queries.GetAuthorizeRequestInfo;

public class GetAuthorizeRequestInfoHandler(IAppDbContext db)
    : IRequestHandler<GetAuthorizeRequestInfoQuery, AuthorizeRequestInfoDto>
{
    public async Task<AuthorizeRequestInfoDto> Handle(GetAuthorizeRequestInfoQuery request, CancellationToken ct)
    {
        // Anonymous query — bypass tenant filter, look up across all tenants.
        var client = await db.OAuthClients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ClientId == request.ClientId && !c.IsRevoked, ct)
            ?? throw new NotFoundException("invalid_client");

        if (client.ClientType != OAuthClientType.Public)
            throw new BadRequestException("Authorization code flow requires a public client.");

        if (!client.IsRedirectUriAllowed(request.RedirectUri))
            throw new BadRequestException("invalid_redirect_uri");

        var requested = (request.Scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Only scopes the client is registered for can be granted.
        var grantable = requested
            .Where(s => client.GetScopes().Contains(s, StringComparer.Ordinal))
            .ToList();

        if (grantable.Count == 0)
            throw new BadRequestException("invalid_scope");

        return new AuthorizeRequestInfoDto(
            ClientId: client.ClientId,
            ClientName: client.Name,
            RequestedScopes: requested,
            GrantableScopes: grantable,
            RedirectUri: request.RedirectUri);
    }
}
