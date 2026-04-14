using MediatR;

namespace SaasApi.Application.Features.Tenants.Commands.CreateTenant
{
    public record CreateTenantCommand(string Name, string Slug) : IRequest<CreateTenantResult>;

    public record CreateTenantResult(Guid TenantId);

}
