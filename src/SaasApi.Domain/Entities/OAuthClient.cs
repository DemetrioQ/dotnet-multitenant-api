using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

/// <summary>
/// Confidential clients hold a secret (server-side: CI scripts, cron jobs).
/// Public clients can't keep a secret (CLI tools, mobile apps, browser SPAs)
/// and must use authorization_code + PKCE.
/// </summary>
public enum OAuthClientType
{
    Confidential = 0,
    Public = 1,
}

public class OAuthClient : BaseEntity
{
    /// <summary>
    /// Null for clients registered via Dynamic Client Registration (RFC 7591).
    /// DCR happens before user auth — the client has no tenant at registration time.
    /// Tenant binds at the authorize step from the user's JWT (stored on the auth code).
    /// </summary>
    public Guid? TenantId { get; private set; }
    public string ClientId { get; private set; } = default!;
    /// <summary>Null for public clients (PKCE-only).</summary>
    public string? ClientSecretHash { get; private set; }
    public string Name { get; private set; } = default!;
    public OAuthClientType ClientType { get; private set; }
    /// <summary>Comma-separated scope list (e.g. "products:read,orders:read"). Empty string = no scopes.</summary>
    public string Scopes { get; private set; } = string.Empty;
    /// <summary>Comma-separated allowed redirect URIs for authorization_code grant. Empty for confidential clients.</summary>
    public string RedirectUris { get; private set; } = string.Empty;
    public bool IsRevoked { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private OAuthClient() { }

    public static OAuthClient CreateConfidential(
        Guid tenantId,
        string clientId,
        string clientSecretHash,
        string name,
        IEnumerable<string> scopes)
    {
        return new OAuthClient
        {
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            Name = name,
            ClientType = OAuthClientType.Confidential,
            Scopes = string.Join(",", scopes.Distinct()),
            RedirectUris = string.Empty,
            IsRevoked = false,
        };
    }

    public static OAuthClient CreatePublic(
        Guid tenantId,
        string clientId,
        string name,
        IEnumerable<string> scopes,
        IEnumerable<string> redirectUris)
    {
        return new OAuthClient
        {
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecretHash = null,
            Name = name,
            ClientType = OAuthClientType.Public,
            Scopes = string.Join(",", scopes.Distinct()),
            RedirectUris = string.Join(",", redirectUris.Distinct()),
            IsRevoked = false,
        };
    }

    /// <summary>
    /// Dynamic Client Registration (RFC 7591): tenantless public client. The client
    /// gains its effective tenant when a user authorizes it — the auth code stores
    /// that user's TenantId, and downstream tokens read it from there.
    /// All DCR clients receive the full configured scope set; per-user consent
    /// would narrow it later if we added a consent screen.
    /// </summary>
    public static OAuthClient CreatePublicForDcr(
        string clientId,
        string name,
        IEnumerable<string> scopes,
        IEnumerable<string> redirectUris)
    {
        return new OAuthClient
        {
            TenantId = null,
            ClientId = clientId,
            ClientSecretHash = null,
            Name = name,
            ClientType = OAuthClientType.Public,
            Scopes = string.Join(",", scopes.Distinct()),
            RedirectUris = string.Join(",", redirectUris.Distinct()),
            IsRevoked = false,
        };
    }

    public IReadOnlyList<string> GetScopes() =>
        string.IsNullOrEmpty(Scopes)
            ? Array.Empty<string>()
            : Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public IReadOnlyList<string> GetRedirectUris() =>
        string.IsNullOrEmpty(RedirectUris)
            ? Array.Empty<string>()
            : RedirectUris.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public bool IsRedirectUriAllowed(string redirectUri) =>
        GetRedirectUris().Contains(redirectUri, StringComparer.Ordinal);

    public void MarkUsed() => LastUsedAt = DateTime.UtcNow;
    public void Revoke() => IsRevoked = true;
}
