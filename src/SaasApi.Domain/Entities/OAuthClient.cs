using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class OAuthClient : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string ClientId { get; private set; } = default!;
    public string ClientSecretHash { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsRevoked { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private OAuthClient() { }

    public static OAuthClient Create(Guid tenantId, string clientId, string clientSecretHash, string name)
    {
        return new OAuthClient
        {
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecretHash = clientSecretHash,
            Name = name,
            IsRevoked = false,
        };
    }

    public void MarkUsed() => LastUsedAt = DateTime.UtcNow;
    public void Revoke() => IsRevoked = true;
}
