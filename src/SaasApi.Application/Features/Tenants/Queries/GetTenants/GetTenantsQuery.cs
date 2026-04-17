using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenants
{
    public record GetTenantsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<TenantDto>>;
}
