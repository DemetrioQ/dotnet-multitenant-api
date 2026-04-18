using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class CustomerEmailVerificationToken : BaseEntity, ITenantEntity
{
    public Guid CustomerId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired => ExpiresAt < DateTime.UtcNow;

    private CustomerEmailVerificationToken() { }

    public static CustomerEmailVerificationToken Create(Guid tenantId, Guid customerId) =>
        new()
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
}
