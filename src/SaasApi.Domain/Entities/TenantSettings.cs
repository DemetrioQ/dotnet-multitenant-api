using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class TenantSettings : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Timezone { get; private set; } = "UTC";
    public string Currency { get; private set; } = "USD";
    public string? SupportEmail { get; private set; }
    public string? WebsiteUrl { get; private set; }

    private TenantSettings() { } // EF Core

    public static TenantSettings Create(Guid tenantId) =>
        new() { TenantId = tenantId };

    public void Update(string timezone, string currency, string? supportEmail, string? websiteUrl)
    {
        Timezone = timezone;
        Currency = currency;
        SupportEmail = supportEmail;
        WebsiteUrl = websiteUrl;
    }
}
