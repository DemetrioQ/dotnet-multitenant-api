using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Users.Queries.GetUsers
{
    public record GetUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserDto>>;
}
