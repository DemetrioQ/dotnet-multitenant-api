using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Features.Tenants.Queries;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Commands.UpdateTenant
{
    public class UpdateTenantHandler(IRepository<Tenant> tenantRepo) : IRequestHandler<UpdateTenantCommand, TenantDto>
    {
        public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken ct)
        {
            var tenant = await tenantRepo.GetByIdAsync(request.Id, ct);
            if (tenant is null)
                throw new NotFoundException("Tenant not found");

            tenant.UpdateName(request.Name);
            tenantRepo.Update(tenant);
            await tenantRepo.SaveChangesAsync(ct);

            return TenantDto.FromEntity(tenant);
        }
    }
}
