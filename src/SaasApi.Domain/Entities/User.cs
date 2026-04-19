using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; } = UserRole.Member;
    public bool IsActive { get; private set; } = true;
    public bool IsEmailVerified { get; private set; }

    private User() { } // EF Core

    public static User Create(Guid tenantId, string email, string passwordHash, UserRole role = UserRole.Member)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email address is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        return new User { TenantId = tenantId, Email = email, PasswordHash = passwordHash, Role = role };
    }

    public void VerifyEmail() => IsEmailVerified = true;

    public void ResetPassword(string newPasswordHash) => PasswordHash = newPasswordHash;

    public void UpdateRole(UserRole role) => Role = role;
    public void Deactivate() => IsActive = false;
}
