using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenants
{
    public class GetTenantsHandler(
        IRepository<Tenant> tenantRepo
        ) : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>
    {
        public async Task<IReadOnlyList<TenantDto>> Handle(GetTenantsQuery request, CancellationToken ct)
        {
            var tenants = await tenantRepo.GetAllAsync(ct);
            return tenants.Select(TenantDto.FromEntity).ToList();
        }
    }
}
