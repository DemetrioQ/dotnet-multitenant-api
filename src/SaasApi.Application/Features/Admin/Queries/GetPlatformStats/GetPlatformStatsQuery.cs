using MediatR;

namespace SaasApi.Application.Features.Admin.Queries.GetPlatformStats;

public record GetPlatformStatsQuery : IRequest<PlatformStatsDto>;

public record PlatformStatsDto(int TotalTenants, int TotalUsers, int TotalProducts);
