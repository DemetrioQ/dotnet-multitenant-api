using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class TenantOnboardingStatus : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public bool ProfileCompleted { get; private set; }
    public bool FirstProductCreated { get; private set; }
    public bool IsComplete => ProfileCompleted && FirstProductCreated;

    private TenantOnboardingStatus() { } // EF Core

    public static TenantOnboardingStatus Create(Guid tenantId) =>
        new() { TenantId = tenantId };

    public void CompleteProfile() => ProfileCompleted = true;
    public void CompleteFirstProduct() => FirstProductCreated = true;
}
