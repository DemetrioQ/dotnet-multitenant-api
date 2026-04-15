using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Queries.GetUsers
{
    public class GetUsersHandler(IRepository<User> userRepo) : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
    {
        public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
        {
            var users = await userRepo.GetAllAsync(ct);
            return users.Select(UserDto.FromEntity).ToList();
        }
    }
}
