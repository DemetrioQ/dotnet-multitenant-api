using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Tenants.Queries
{
    public record TenantDto(Guid Id, string Name, string Slug, bool IsActive, DateTime createdAt)
    {
        public static TenantDto FromEntity(Tenant tenant) =>
            new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAt);
    }
}
