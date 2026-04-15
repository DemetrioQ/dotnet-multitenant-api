using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = "member"; // e.g. admin, member
    public bool IsActive { get; private set; } = true;

    private User() { } // EF Core

    public static User Create(Guid tenantId, string email, string passwordHash, string role = "member")
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email address is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        return new User { TenantId = tenantId, Email = email, PasswordHash = passwordHash, Role = role };
    }

    public void UpdateRole(string role) => Role = role;
    public void Deactivate() => IsActive = false;

}
