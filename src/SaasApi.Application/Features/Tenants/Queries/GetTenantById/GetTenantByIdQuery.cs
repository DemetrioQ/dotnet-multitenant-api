using MediatR;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantById
{
    public record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto>;
}
