using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Commands.DeactivateTenant
{
    public class DeactivateTenantHandler(
        IRepository<Tenant> tenantRepo,
        IAuditService auditService,
        IMemoryCache cache) : IRequestHandler<DeactivateTenantCommand>
    {
        public async Task Handle(DeactivateTenantCommand request, CancellationToken ct)
        {
            var tenant = await tenantRepo.GetByIdAsync(request.Id, ct);
            if (tenant is null)
                throw new NotFoundException("Tenant not found");

            tenant.Deactivate();
            tenantRepo.Update(tenant);
            await tenantRepo.SaveChangesAsync(ct);

            cache.Remove($"tenant:{tenant.Id}");

            await auditService.LogAsync("tenant.deactivated", "Tenant", tenant.Id, ct: ct);
        }
    }
}
