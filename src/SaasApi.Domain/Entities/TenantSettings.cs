using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class TenantSettings : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string? SupportEmail { get; private set; }
    public string? WebsiteUrl { get; private set; }

    private TenantSettings() { } // EF Core

    public static TenantSettings Create(Guid tenantId) =>
        new() { TenantId = tenantId };

    public void Update(string? supportEmail, string? websiteUrl)
    {
        SupportEmail = supportEmail;
        WebsiteUrl = websiteUrl;
    }
}
