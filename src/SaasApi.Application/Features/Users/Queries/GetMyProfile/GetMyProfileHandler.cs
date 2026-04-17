using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Queries.GetMyProfile;

public class GetMyProfileHandler(
    IRepository<User> userRepo,
    IRepository<UserProfile> profileRepo,
    ICurrentUserService currentUserService,
    IMemoryCache cache)
    : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var userId = currentUserService.UserId;
        var cacheKey = $"profile:{userId}";
        if (cache.TryGetValue(cacheKey, out UserProfileDto? cached))
            return cached!;

        var users = await userRepo.FindAsync(u => u.Id == userId, ct);
        var user = users.FirstOrDefault() ?? throw new NotFoundException("User not found.");

        var profiles = await profileRepo.FindAsync(p => p.UserId == userId, ct);
        var profile = profiles.FirstOrDefault() ?? throw new NotFoundException("Profile not found.");

        var dto = UserProfileDto.FromEntities(user, profile);
        cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));

        return dto;
    }
}
