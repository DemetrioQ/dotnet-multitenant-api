using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Queries.GetTenantById
{
    public class GetTenantByIdHandler(
        IRepository<Tenant> tenantRepo,
        IRepository<TenantSettings> settingsRepo,
        IMemoryCache cache)
        : IRequestHandler<GetTenantByIdQuery, TenantDto>
    {
        public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken ct)
        {
            var cacheKey = $"tenant:{request.Id}";
            if (cache.TryGetValue(cacheKey, out TenantDto? cached))
                return cached!;

            var tenant = await tenantRepo.GetByIdAsync(request.Id, ct);
            if (tenant is null)
                throw new NotFoundException("Tenant not found");

            var settings = (await settingsRepo.FindGlobalAsync(s => s.TenantId == request.Id, ct)).FirstOrDefault();
            if (settings is null)
                throw new NotFoundException("Tenant settings not found");

            var dto = TenantDto.FromEntities(tenant, settings);
            cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));

            return dto;
        }
    }
}
