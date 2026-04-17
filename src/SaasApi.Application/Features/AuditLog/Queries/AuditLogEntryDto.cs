using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.AuditLog.Queries;

public record AuditLogEntryDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string UserEmail,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? Details,
    DateTime CreatedAt)
{
    public static AuditLogEntryDto FromEntity(AuditLogEntry entry) =>
        new(entry.Id, entry.TenantId, entry.UserId, entry.UserEmail, entry.Action, entry.EntityType, entry.EntityId, entry.Details, entry.CreatedAt);
}
