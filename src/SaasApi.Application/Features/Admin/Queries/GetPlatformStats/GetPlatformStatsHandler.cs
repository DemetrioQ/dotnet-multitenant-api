using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Admin.Queries.GetPlatformStats;

public class GetPlatformStatsHandler(
    IRepository<Tenant> tenantRepo,
    IRepository<User> userRepo,
    IRepository<Product> productRepo)
    : IRequestHandler<GetPlatformStatsQuery, PlatformStatsDto>
{
    public async Task<PlatformStatsDto> Handle(GetPlatformStatsQuery request, CancellationToken ct)
    {
        var tenants = await tenantRepo.FindGlobalAsync(_ => true, ct);
        var users = await userRepo.FindGlobalAsync(_ => true, ct);
        var products = await productRepo.FindGlobalAsync(_ => true, ct);

        return new PlatformStatsDto(tenants.Count, users.Count, products.Count);
    }
}
