using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Users.Queries;

public record UserProfileDto(
    Guid UserId,
    string Email,
    string Role,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    string? Bio,
    bool IsComplete)
{
    public static UserProfileDto FromEntities(User user, UserProfile profile) =>
        new(user.Id, user.Email, user.Role.ToDbString(), profile.FirstName, profile.LastName, profile.AvatarUrl, profile.Bio, profile.IsComplete);
}
