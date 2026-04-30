using MediatR;
using SaasApi.Application.Common.Auth;
using SaasApi.Domain.Common;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterDynamicClient;

public class RegisterDynamicClientHandler(IRepository<OAuthClient> clientRepo)
    : IRequestHandler<RegisterDynamicClientCommand, RegisterDynamicClientResult>
{
    public async Task<RegisterDynamicClientResult> Handle(RegisterDynamicClientCommand request, CancellationToken ct)
    {
        var clientId = "saasapi_" + SecureRandom.UrlSafeToken(24);
        var redirectUris = request.RedirectUris!.Distinct().ToList();
        var name = string.IsNullOrWhiteSpace(request.ClientName) ? "Dynamic Client" : request.ClientName!;

        // DCR clients receive the full configured scope set. Per-user consent
        // narrowing happens at the authorize step, not at registration.
        var entity = OAuthClient.CreatePublicForDcr(
            clientId: clientId,
            name: name,
            scopes: OAuthScopes.All,
            redirectUris: redirectUris);

        await clientRepo.AddAsync(entity, ct);
        await clientRepo.SaveChangesAsync(ct);

        return new RegisterDynamicClientResult(
            ClientId: clientId,
            ClientIdIssuedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ClientName: name,
            RedirectUris: redirectUris,
            GrantTypes: ["authorization_code", "refresh_token"],
            ResponseTypes: ["code"],
            TokenEndpointAuthMethod: "none",
            Scope: string.Join(' ', OAuthScopes.All));
    }
}
