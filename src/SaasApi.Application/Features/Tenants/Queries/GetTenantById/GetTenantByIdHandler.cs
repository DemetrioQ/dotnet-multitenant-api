using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantById
{
    public class GetTenantByIdHandler(IRepository<Tenant> tenantRepo) : IRequestHandler<GetTenantByIdQuery, TenantDto>
    {
        public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken ct)
        {
            var tenant = await tenantRepo.GetByIdAsync(request.Id, ct);
            if (tenant is null)
                throw new NotFoundException("Tenant not found");

            return TenantDto.FromEntity(tenant);
        }
    }
}
