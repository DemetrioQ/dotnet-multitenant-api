using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Queries.GetStorefrontInfo
{
    public class GetStorefrontInfoHandler(
        IRepository<Tenant> tenantRepo,
        IRepository<TenantSettings> settingsRepo,
        ICurrentTenantService currentTenantService
        ) : IRequestHandler<GetStorefrontInfoQuery, StorefrontInfoDto>
    {
        public async Task<StorefrontInfoDto> Handle(GetStorefrontInfoQuery request, CancellationToken ct)
        {
            var tenant = await tenantRepo.GetByIdAsync(currentTenantService.TenantId, ct);
            if (tenant is null || !tenant.IsActive)
                throw new NotFoundException("Store not found.");

            var settingsList = await settingsRepo.FindAsync(_ => true, ct);
            var settings = settingsList.FirstOrDefault();

            return new StorefrontInfoDto(
                tenant.Name,
                tenant.Slug,
                settings?.Currency ?? "USD",
                settings?.Timezone ?? "UTC",
                settings?.SupportEmail,
                settings?.WebsiteUrl);
        }
    }
}
