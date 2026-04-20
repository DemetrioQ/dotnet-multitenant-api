using MediatR;

namespace SaasApi.Application.Features.Admin.Queries.GetPlatformStats;

public record GetPlatformStatsQuery : IRequest<PlatformStatsDto>;

public record PlatformStatsDto(
    int TotalTenants,
    int ActiveTenants,
    int InactiveTenants,
    int NewTenantsThisWeek,
    int TotalUsers,
    int TotalProducts);
