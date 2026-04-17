using SaasApi.Domain.Common;
using System.Security.Cryptography;

namespace SaasApi.Domain.Entities;

public class Invitation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
    public bool IsAccepted => AcceptedAt.HasValue;
    public bool IsPending => !IsAccepted && !IsExpired;

    private Invitation() { } // EF Core

    public static Invitation Create(Guid tenantId, string email) =>
        new()
        {
            TenantId = tenantId,
            Email = email,
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

    public void Accept() => AcceptedAt = DateTime.UtcNow;
}
