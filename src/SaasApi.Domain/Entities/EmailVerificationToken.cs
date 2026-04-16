using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class EmailVerificationToken : BaseEntity, ITenantEntity
{
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired => ExpiresAt < DateTime.UtcNow;

    private EmailVerificationToken() { } // EF Core

    public static EmailVerificationToken Create(Guid tenantId, Guid userId)
    {
        return new EmailVerificationToken
        {
            TenantId = tenantId,
            UserId = userId,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }
}
