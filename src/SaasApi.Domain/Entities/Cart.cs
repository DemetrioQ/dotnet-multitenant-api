using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class Cart : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }

    private Cart() { }

    public static Cart Create(Guid tenantId, Guid customerId) =>
        new() { TenantId = tenantId, CustomerId = customerId };

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
