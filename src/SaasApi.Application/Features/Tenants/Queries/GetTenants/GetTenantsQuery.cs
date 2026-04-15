using MediatR;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenants
{
    public record GetTenantsQuery : IRequest<IReadOnlyList<TenantDto>>;
}
