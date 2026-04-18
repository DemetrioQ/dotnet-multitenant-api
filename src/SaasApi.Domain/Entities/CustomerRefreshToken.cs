using SaasApi.Domain.Common;
using System.Security.Cryptography;

namespace SaasApi.Domain.Entities;

public class CustomerRefreshToken : BaseEntity, ITenantEntity
{
    public string Token { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid FamilyId { get; private set; }

    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
    public bool IsValid => RevokedAt == null && !IsExpired;

    private CustomerRefreshToken() { }

    public static CustomerRefreshToken Create(Guid tenantId, Guid customerId, Guid familyId) =>
        new()
        {
            TenantId = tenantId,
            CustomerId = customerId,
            FamilyId = familyId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

    public void Revoke() => RevokedAt = DateTime.UtcNow;
}
