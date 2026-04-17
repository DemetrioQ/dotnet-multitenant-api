using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class AuditLogEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserEmail { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string? Details { get; private set; }

    private AuditLogEntry() { } // EF Core

    public static AuditLogEntry Create(Guid tenantId, Guid userId, string userEmail, string action, string entityType, Guid? entityId, string? details = null) =>
        new()
        {
            TenantId = tenantId,
            UserId = userId,
            UserEmail = userEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        };
}
