using MediatR;

namespace SaasApi.Application.Features.Users.Queries.GetUsers
{
    public record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>;
}
