using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

/// <summary>
/// Refresh token issued at the authorization_code exchange.
/// Rotated on each /oauth/token refresh call: the old token is revoked
/// and the new one's <see cref="ReplacedByToken"/> chains the rotation
/// so a stolen-and-replayed token can be detected (the new token was
/// already issued, replaying the old one indicates compromise).
/// </summary>
public class OAuthRefreshToken : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Token { get; private set; } = default!;
    public Guid OAuthClientId { get; private set; }
    public Guid UserId { get; private set; }
    public string Scopes { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }

    private OAuthRefreshToken() { }

    public static OAuthRefreshToken Issue(
        Guid tenantId,
        string token,
        Guid oauthClientId,
        Guid userId,
        IEnumerable<string> scopes,
        TimeSpan lifetime)
    {
        return new OAuthRefreshToken
        {
            TenantId = tenantId,
            Token = token,
            OAuthClientId = oauthClientId,
            UserId = userId,
            Scopes = string.Join(",", scopes.Distinct()),
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            IsRevoked = false,
        };
    }

    public IReadOnlyList<string> GetScopes() =>
        string.IsNullOrEmpty(Scopes)
            ? Array.Empty<string>()
            : Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
    }
}
