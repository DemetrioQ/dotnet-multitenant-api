using MediatR;
using SaasApi.Application.Features.Users.Queries;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Users.Commands.UpdateUserRole
{
    public record UpdateUserRoleCommand(Guid Id, UserRole Role) : IRequest<UserDto>;
}
