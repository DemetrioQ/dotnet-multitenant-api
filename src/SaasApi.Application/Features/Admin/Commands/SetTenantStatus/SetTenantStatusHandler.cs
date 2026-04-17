using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Admin.Commands.SetTenantStatus;

public class SetTenantStatusHandler(
    IRepository<Tenant> tenantRepo,
    IMemoryCache cache)
    : IRequestHandler<SetTenantStatusCommand>
{
    public async Task Handle(SetTenantStatusCommand request, CancellationToken ct)
    {
        var tenant = await tenantRepo.GetByIdAsync(request.TenantId, ct);
        if (tenant is null)
            throw new NotFoundException("Tenant not found");

        if (request.IsActive)
            tenant.Activate();
        else
            tenant.Deactivate();

        tenantRepo.Update(tenant);
        await tenantRepo.SaveChangesAsync(ct);

        cache.Remove($"tenant:{tenant.Id}");
    }
}
