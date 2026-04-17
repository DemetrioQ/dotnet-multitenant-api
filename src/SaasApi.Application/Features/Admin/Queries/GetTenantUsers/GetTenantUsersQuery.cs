using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.Users.Queries;

namespace SaasApi.Application.Features.Admin.Queries.GetTenantUsers;

public record GetTenantUsersQuery(Guid TenantId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserDto>>;
