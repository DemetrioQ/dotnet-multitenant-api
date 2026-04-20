using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Features.Tenants.Queries;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Commands.UpdateTenant
{
    public class UpdateTenantHandler(
        IRepository<Tenant> tenantRepo,
        IRepository<TenantSettings> settingsRepo,
        IAuditService auditService,
        IStoreUrlBuilder storeUrlBuilder,
        IMemoryCache cache)
        : IRequestHandler<UpdateTenantCommand, TenantDto>
    {
        public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken ct)
        {
            var tenant = await tenantRepo.GetByIdAsync(request.Id, ct);
            if (tenant is null)
                throw new NotFoundException("Tenant not found");

            var settings = (await settingsRepo.FindGlobalAsync(s => s.TenantId == request.Id, ct)).FirstOrDefault();
            if (settings is null)
                throw new NotFoundException("Tenant settings not found");

            tenant.UpdateName(request.Name);
            settings.Update(request.SupportEmail, request.WebsiteUrl);

            tenantRepo.Update(tenant);
            settingsRepo.Update(settings);
            await tenantRepo.SaveChangesAsync(ct);

            cache.Remove($"tenant:{tenant.Id}");

            await auditService.LogAsync("tenant.updated", "Tenant", tenant.Id, ct: ct);

            return TenantDto.FromEntities(tenant, settings, storeUrlBuilder.BuildUrl(tenant.Slug));
        }
    }
}
