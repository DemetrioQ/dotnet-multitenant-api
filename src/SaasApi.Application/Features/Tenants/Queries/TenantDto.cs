using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Tenants.Queries
{
    public record TenantDto(
        Guid Id,
        string Name,
        string Slug,
        bool IsActive,
        DateTime CreatedAt,
        string StoreUrl,
        string? SupportEmail,
        string? WebsiteUrl)
    {
        public static TenantDto FromEntities(Tenant tenant, TenantSettings settings, string storeUrl) =>
            new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAt, storeUrl,
                settings.SupportEmail, settings.WebsiteUrl);
    }
}
