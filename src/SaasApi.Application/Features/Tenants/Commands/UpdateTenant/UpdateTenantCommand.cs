using MediatR;
using SaasApi.Application.Features.Tenants.Queries;

namespace SaasApi.Application.Features.Tenants.Commands.UpdateTenant
{
    public record UpdateTenantCommand(Guid Id, string Name) : IRequest<TenantDto>;

}
