using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Auth;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Commands.RefreshAccessToken;

public class RefreshAccessTokenHandler(
    IAppDbContext db,
    IRepository<OAuthRefreshToken> refreshTokenRepo,
    IJwtTokenService jwt) : IRequestHandler<RefreshAccessTokenCommand, RefreshAccessTokenResult>
{
    private const int AccessTokenLifetimeSeconds = 3600;
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    public async Task<RefreshAccessTokenResult> Handle(RefreshAccessTokenCommand request, CancellationToken ct)
    {
        var existing = await db.OAuthRefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("invalid_grant");

        // Stolen-token detection: a refresh attempt on a previously-rotated
        // (revoked) token implies the legitimate client has already moved on,
        // so revoke the entire chain.
        if (existing.IsRevoked)
        {
            await RevokeChainAsync(existing.UserId, existing.OAuthClientId, ct);
            throw new UnauthorizedAccessException("invalid_grant");
        }

        if (existing.IsExpired)
            throw new UnauthorizedAccessException("invalid_grant");

        var client = await db.OAuthClients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == existing.OAuthClientId && !c.IsRevoked, ct)
            ?? throw new UnauthorizedAccessException("invalid_client");

        if (!string.Equals(client.ClientId, request.ClientId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("invalid_client");

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == existing.UserId && u.IsActive, ct)
            ?? throw new UnauthorizedAccessException("invalid_grant");

        var scopes = existing.GetScopes();

        // Rotate: new refresh token, mark old as replaced.
        var newToken = SecureRandom.UrlSafeToken(48);
        var rotated = OAuthRefreshToken.Issue(
            tenantId: existing.TenantId,
            token: newToken,
            oauthClientId: existing.OAuthClientId,
            userId: existing.UserId,
            scopes: scopes,
            lifetime: RefreshLifetime);
        existing.Revoke(replacedByToken: newToken);
        await refreshTokenRepo.AddAsync(rotated, ct);
        await refreshTokenRepo.SaveChangesAsync(ct);

        var accessToken = jwt.GenerateAuthorizationCodeToken(user, client, scopes);

        return new RefreshAccessTokenResult(
            AccessToken: accessToken,
            RefreshToken: newToken,
            ExpiresInSeconds: AccessTokenLifetimeSeconds,
            Scope: string.Join(' ', scopes));
    }

    private async Task RevokeChainAsync(Guid userId, Guid clientId, CancellationToken ct)
    {
        var chain = await db.OAuthRefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.OAuthClientId == clientId && !t.IsRevoked)
            .ToListAsync(ct);
        foreach (var t in chain) t.Revoke();
        await refreshTokenRepo.SaveChangesAsync(ct);
    }
}
