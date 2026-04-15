using MediatR;

namespace SaasApi.Application.Features.Users.Commands.DeactivateUser
{
    public record DeactivateUserCommand(Guid Id) : IRequest;
}
