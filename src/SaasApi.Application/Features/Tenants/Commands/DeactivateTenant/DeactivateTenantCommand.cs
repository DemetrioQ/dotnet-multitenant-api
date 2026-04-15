using MediatR;

namespace SaasApi.Application.Features.Tenants.Commands.DeactivateTenant
{
    public record DeactivateTenantCommand(Guid Id) : IRequest;

}
