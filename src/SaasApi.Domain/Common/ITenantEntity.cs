namespace SaasApi.Domain.Common;

/// <summary>
/// Marks an entity as belonging to a specific tenant.
/// Used by the global query filter in AppDbContext to enforce tenant isolation.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; }
}
