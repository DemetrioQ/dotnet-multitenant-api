using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.Users.Queries;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Admin.Queries.GetTenantUsers;

public class GetTenantUsersHandler(IRepository<User> userRepo)
    : IRequestHandler<GetTenantUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetTenantUsersQuery request, CancellationToken ct)
    {
        var allUsers = await userRepo.FindGlobalAsync(u => u.TenantId == request.TenantId, ct);
        var totalCount = allUsers.Count;

        var skip = (request.Page - 1) * request.PageSize;
        var paged = allUsers.Skip(skip).Take(request.PageSize).ToList();
        var dtos = paged.Select(UserDto.FromEntity).ToList();

        return new PagedResult<UserDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
