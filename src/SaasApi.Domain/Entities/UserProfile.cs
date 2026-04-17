using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class UserProfile : BaseEntity, ITenantEntity
{
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }

    public bool IsComplete => !string.IsNullOrWhiteSpace(FirstName);

    private UserProfile() { } // EF Core

    public static UserProfile Create(Guid userId, Guid tenantId, string firstName, string lastName) =>
        new() { UserId = userId, TenantId = tenantId, FirstName = firstName, LastName = lastName };

    public void Update(string? firstName, string? lastName, string? avatarUrl, string? bio)
    {
        FirstName = firstName;
        LastName = lastName;
        AvatarUrl = avatarUrl;
        Bio = bio;
    }
}
