using System.Security.Cryptography;
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

        var clientId = "saasapi_" + GenerateUrlSafeRandom(24);
        var clientSecret = GenerateUrlSafeRandom(48);

        var entity = OAuthClient.Create(
            tenantService.TenantId,
            clientId,
            hasher.Hash(clientSecret),
            request.Name);

        await clientRepo.AddAsync(entity, ct);
        await clientRepo.SaveChangesAsync(ct);

        return new RegisterOAuthClientResult(clientId, clientSecret, request.Name);
    }

    private static string GenerateUrlSafeRandom(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
