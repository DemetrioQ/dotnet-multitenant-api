using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Infrastructure.Services;

public class AuditService(
    IRepository<AuditLogEntry> auditRepo,
    ICurrentTenantService tenantService,
    ICurrentUserService userService) : IAuditService
{
    public async Task LogAsync(string action, string entityType, Guid? entityId, string? details = null, CancellationToken ct = default)
    {
        var entry = AuditLogEntry.Create(
            tenantService.TenantId,
            userService.UserId,
            userService.Email ?? "unknown",
            action,
            entityType,
            entityId,
            details);

        await auditRepo.AddAsync(entry, ct);
        await auditRepo.SaveChangesAsync(ct);
    }
}
