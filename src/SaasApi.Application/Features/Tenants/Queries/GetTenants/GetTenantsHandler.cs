using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenants
{
    public class GetTenantsHandler(
        IRepository<Tenant> tenantRepo,
        IRepository<TenantSettings> settingsRepo,
        IStoreUrlBuilder storeUrlBuilder)
        : IRequestHandler<GetTenantsQuery, PagedResult<TenantDto>>
    {
        public async Task<PagedResult<TenantDto>> Handle(GetTenantsQuery request, CancellationToken ct)
        {
            int skip = (request.Page - 1) * request.PageSize;
            var tenants = await tenantRepo.GetPagedAsync(skip, request.PageSize, ct);
            int totalCount = await tenantRepo.CountAsync(ct);

            var allSettings = await settingsRepo.FindGlobalAsync(_ => true, ct);
            var settingsMap = allSettings.ToDictionary(s => s.TenantId);

            var dtos = tenants
                .Where(t => settingsMap.ContainsKey(t.Id))
                .Select(t => TenantDto.FromEntities(t, settingsMap[t.Id], storeUrlBuilder.BuildUrl(t.Slug)))
                .ToList();

            return new PagedResult<TenantDto>(dtos, totalCount, request.Page, request.PageSize);
        }
    }
}
