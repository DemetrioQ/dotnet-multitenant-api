using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Queries.GetUsers
{
    public class GetUsersHandler(IRepository<User> userRepo)
        : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
        {
            int skip = (request.Page - 1) * request.PageSize;
            var users = await userRepo.GetPagedAsync(skip, request.PageSize, ct);
            int totalCount = await userRepo.CountAsync(ct);

            var dtos = users.Select(UserDto.FromEntity).ToList();

            return new PagedResult<UserDto>(dtos, totalCount, request.Page, request.PageSize);
        }
    }
}
