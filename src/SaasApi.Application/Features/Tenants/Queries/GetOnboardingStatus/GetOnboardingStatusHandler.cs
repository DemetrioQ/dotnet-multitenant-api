using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Queries.GetOnboardingStatus;

public class GetOnboardingStatusHandler(
    IRepository<TenantOnboardingStatus> onboardingRepo,
    ICurrentTenantService tenantService,
    IMemoryCache cache)
    : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery request, CancellationToken ct)
    {
        var cacheKey = $"onboarding:{tenantService.TenantId}";
        if (cache.TryGetValue(cacheKey, out OnboardingStatusDto? cached))
            return cached!;

        var statuses = await onboardingRepo.FindAsync(_ => true, ct);
        var status = statuses.FirstOrDefault()
            ?? throw new NotFoundException("Onboarding status not found.");

        var dto = OnboardingStatusDto.FromEntity(status);
        cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));

        return dto;
    }
}
