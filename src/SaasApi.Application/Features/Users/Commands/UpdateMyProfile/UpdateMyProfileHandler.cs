using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.UpdateMyProfile;

public class UpdateMyProfileHandler(
    IRepository<UserProfile> profileRepo,
    IRepository<TenantOnboardingStatus> onboardingRepo,
    ICurrentUserService currentUserService,
    ICurrentTenantService currentTenantService,
    IAuditService auditService,
    IMemoryCache cache)
    : IRequestHandler<UpdateMyProfileCommand, Unit>
{
    public async Task<Unit> Handle(UpdateMyProfileCommand request, CancellationToken ct)
    {
        var userId = currentUserService.UserId;

        var profiles = await profileRepo.FindAsync(p => p.UserId == userId, ct);
        var profile = profiles.FirstOrDefault() ?? throw new NotFoundException("Profile not found.");

        profile.Update(request.FirstName, request.LastName, request.AvatarUrl, request.Bio);

        if (profile.IsComplete)
        {
            var tenantId = currentTenantService.TenantId;
            var statuses = await onboardingRepo.FindAsync(s => s.TenantId == tenantId, ct);
            var status = statuses.FirstOrDefault();
            if (status is not null && !status.ProfileCompleted)
                status.CompleteProfile();
        }

        await profileRepo.SaveChangesAsync(ct);

        cache.Remove($"profile:{userId}");
        cache.Remove($"onboarding:{currentTenantService.TenantId}");

        await auditService.LogAsync("profile.updated", "UserProfile", profile.Id,
            $"Updated profile: {profile.FirstName} {profile.LastName}", ct);

        return Unit.Value;
    }
}
