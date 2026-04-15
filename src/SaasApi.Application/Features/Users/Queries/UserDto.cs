using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Users.Queries
{
    public record UserDto(Guid Id, string Email, string Role, bool IsActive)
    {
        public static UserDto FromEntity(User user) =>
            new(user.Id, user.Email, user.Role, user.IsActive);
    }
}
