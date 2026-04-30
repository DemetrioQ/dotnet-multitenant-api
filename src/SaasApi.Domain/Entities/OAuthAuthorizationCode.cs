using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

/// <summary>
/// Short-lived (5 min) one-time authorization code minted at the consent step.
/// Exchanged at /oauth/token for an access_token + refresh_token. PKCE
/// (RFC 7636) verifier hash stored here, verifier supplied at exchange time.
/// </summary>
public class OAuthAuthorizationCode : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = default!;
    public Guid OAuthClientId { get; private set; }
    public Guid UserId { get; private set; }
    public string RedirectUri { get; private set; } = default!;
    public string CodeChallenge { get; private set; } = default!;
    public string CodeChallengeMethod { get; private set; } = default!;
    /// <summary>Comma-separated scopes the user actually consented to.</summary>
    public string Scopes { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    private OAuthAuthorizationCode() { }

    public static OAuthAuthorizationCode Issue(
        Guid tenantId,
        string code,
        Guid oauthClientId,
        Guid userId,
        string redirectUri,
        string codeChallenge,
        string codeChallengeMethod,
        IEnumerable<string> scopes,
        TimeSpan lifetime)
    {
        return new OAuthAuthorizationCode
        {
            TenantId = tenantId,
            Code = code,
            OAuthClientId = oauthClientId,
            UserId = userId,
            RedirectUri = redirectUri,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            Scopes = string.Join(",", scopes.Distinct()),
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
        };
    }

    public IReadOnlyList<string> GetScopes() =>
        string.IsNullOrEmpty(Scopes)
            ? Array.Empty<string>()
            : Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public bool IsConsumed => ConsumedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public void Consume() => ConsumedAt = DateTime.UtcNow;
}
