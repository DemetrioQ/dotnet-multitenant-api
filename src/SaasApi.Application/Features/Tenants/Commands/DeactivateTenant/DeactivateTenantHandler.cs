using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Commands.DeactivateTenant
{
    public class DeactivateTenantHandler(IRepository<Tenant> tenantRepo) : IRequestHandler<DeactivateTenantCommand>
    {
        public async Task Handle(DeactivateTenantCommand request, CancellationToken ct)
        {
            var tenant = await tenantRepo.GetByIdAsync(request.Id, ct);
            if (tenant is null)
                throw new NotFoundException("Tenant not found");

            tenant.Deactivate();
            tenantRepo.Update(tenant);
            await tenantRepo.SaveChangesAsync(ct);
        }
    }
}
