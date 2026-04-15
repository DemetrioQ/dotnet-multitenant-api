using MediatR;
using SaasApi.Application.Features.Users.Queries;

namespace SaasApi.Application.Features.Users.Commands.UpdateUserRole
{
    public record UpdateUserRoleCommand(Guid Id, string Role) : IRequest<UserDto>;

}
