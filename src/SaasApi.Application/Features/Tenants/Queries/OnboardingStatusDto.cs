using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Tenants.Queries;

public record OnboardingStatusDto(bool ProfileCompleted, bool FirstProductCreated, bool IsComplete)
{
    public static OnboardingStatusDto FromEntity(TenantOnboardingStatus status) =>
        new(status.ProfileCompleted, status.FirstProductCreated, status.IsComplete);
}
