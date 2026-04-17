using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Tenants.Queries
{
    public record TenantDto(
        Guid Id,
        string Name,
        string Slug,
        bool IsActive,
        DateTime CreatedAt,
        string Timezone,
        string Currency,
        string? SupportEmail,
        string? WebsiteUrl)
    {
        public static TenantDto FromEntities(Tenant tenant, TenantSettings settings) =>
            new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAt,
                settings.Timezone, settings.Currency, settings.SupportEmail, settings.WebsiteUrl);
    }
}
