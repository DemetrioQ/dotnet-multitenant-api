using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Auth;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Commands.ExchangeAuthorizationCode;

public class ExchangeAuthorizationCodeHandler(
    IAppDbContext db,
    IRepository<OAuthRefreshToken> refreshTokenRepo,
    IJwtTokenService jwt) : IRequestHandler<ExchangeAuthorizationCodeCommand, ExchangeAuthorizationCodeResult>
{
    private const int AccessTokenLifetimeSeconds = 3600;        // 1 hour
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    public async Task<ExchangeAuthorizationCodeResult> Handle(ExchangeAuthorizationCodeCommand request, CancellationToken ct)
    {
        // Anonymous endpoint — bypass tenant filter to find the auth code.
        var authCode = await db.OAuthAuthorizationCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Code == request.Code, ct)
            ?? throw new UnauthorizedAccessException("invalid_grant");

        // One-shot, time-limited, redirect_uri-bound.
        if (authCode.IsConsumed || authCode.IsExpired)
            throw new UnauthorizedAccessException("invalid_grant");
        if (!string.Equals(authCode.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("invalid_grant");

        // Look up the associated client and user (filters bypassed — we resolve cross-tenant by ID).
        var client = await db.OAuthClients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == authCode.OAuthClientId && !c.IsRevoked, ct)
            ?? throw new UnauthorizedAccessException("invalid_client");

        if (!string.Equals(client.ClientId, request.ClientId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("invalid_client");

        // PKCE: verifier must hash to the stored challenge under the recorded method.
        if (!PkceVerifier.Verify(request.CodeVerifier, authCode.CodeChallenge, authCode.CodeChallengeMethod))
            throw new UnauthorizedAccessException("invalid_grant");

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == authCode.UserId && u.IsActive, ct)
            ?? throw new UnauthorizedAccessException("invalid_grant");

        // Burn the code — single use.
        authCode.Consume();

        // Issue tokens.
        var scopes = authCode.GetScopes();
        var accessToken = jwt.GenerateAuthorizationCodeToken(user, client, scopes);

        var refreshTokenStr = SecureRandom.UrlSafeToken(48);
        var refreshToken = OAuthRefreshToken.Issue(
            tenantId: user.TenantId,
            token: refreshTokenStr,
            oauthClientId: client.Id,
            userId: user.Id,
            scopes: scopes,
            lifetime: RefreshLifetime);
        await refreshTokenRepo.AddAsync(refreshToken, ct);

        client.MarkUsed();
        await refreshTokenRepo.SaveChangesAsync(ct);

        return new ExchangeAuthorizationCodeResult(
            AccessToken: accessToken,
            RefreshToken: refreshTokenStr,
            ExpiresInSeconds: AccessTokenLifetimeSeconds,
            Scope: string.Join(' ', scopes));
    }
}
