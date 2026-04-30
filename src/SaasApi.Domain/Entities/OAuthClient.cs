using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class OAuthClient : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string ClientId { get; private set; } = default!;
    public string ClientSecretHash { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    /// <summary>Comma-separated scope list (e.g. "products:read,orders:read"). Empty string = no scopes.</summary>
    public string Scopes { get; private set; } = string.Empty;
    public bool IsRevoked { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private OAuthClient() { }

    public static OAuthClient Create(
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
            Scopes = string.Join(",", scopes.Distinct()),
            IsRevoked = false,
        };
    }

    public IReadOnlyList<string> GetScopes() =>
        string.IsNullOrEmpty(Scopes)
            ? Array.Empty<string>()
            : Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public void MarkUsed() => LastUsedAt = DateTime.UtcNow;
    public void Revoke() => IsRevoked = true;
}
