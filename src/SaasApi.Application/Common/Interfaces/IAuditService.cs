namespace SaasApi.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId, string? details = null, CancellationToken ct = default);
}
