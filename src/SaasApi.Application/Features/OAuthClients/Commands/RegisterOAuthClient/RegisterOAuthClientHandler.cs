using SaasApi.Application.Common.Auth;
using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;

public class RegisterOAuthClientHandler(
    IRepository<OAuthClient> clientRepo,
    ICurrentTenantService tenantService,
    IPasswordHasher hasher) : IRequestHandler<RegisterOAuthClientCommand, RegisterOAuthClientResult>
{
    public async Task<RegisterOAuthClientResult> Handle(RegisterOAuthClientCommand request, CancellationToken ct)
    {
        if (!tenantService.IsResolved)
            throw new UnauthorizedAccessException("Tenant context required.");

        var clientId = "saasapi_" + SecureRandom.UrlSafeToken(24);

        OAuthClient entity;
        string? secretToReturn;

        if (request.ClientType == "public")
        {
            entity = OAuthClient.CreatePublic(
                tenantId: tenantService.TenantId,
                clientId: clientId,
                name: request.Name,
                scopes: request.Scopes,
                redirectUris: request.RedirectUris ?? Array.Empty<string>());
            secretToReturn = null;
        }
        else
        {
            var clientSecret = SecureRandom.UrlSafeToken(48);
            entity = OAuthClient.CreateConfidential(
                tenantId: tenantService.TenantId,
                clientId: clientId,
                clientSecretHash: hasher.Hash(clientSecret),
                name: request.Name,
                scopes: request.Scopes);
            secretToReturn = clientSecret;
        }

        await clientRepo.AddAsync(entity, ct);
        await clientRepo.SaveChangesAsync(ct);

        return new RegisterOAuthClientResult(
            ClientId: clientId,
            ClientSecret: secretToReturn,
            Name: request.Name,
            ClientType: request.ClientType,
            Scopes: entity.GetScopes(),
            RedirectUris: entity.GetRedirectUris());
    }
}
