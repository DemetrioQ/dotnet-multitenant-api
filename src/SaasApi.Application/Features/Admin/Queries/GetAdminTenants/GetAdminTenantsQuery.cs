using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Admin.Queries.GetAdminTenants;

public record GetAdminTenantsQuery(int Page, int PageSize) : IRequest<PagedResult<AdminTenantDto>>;

public record AdminTenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    int ProductCount,
    DateTime? LastActivityAt);
