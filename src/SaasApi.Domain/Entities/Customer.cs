using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public bool IsEmailVerified { get; private set; }
    public bool IsDemo { get; private set; }
    public DateTime? DemoExpiresAt { get; private set; }

    private Customer() { }

    public static Customer Create(Guid tenantId, string email, string passwordHash, string firstName, string lastName)
    {
        Validate(email, passwordHash);
        return new Customer
        {
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName
        };
    }

    public static Customer CreateDemo(Guid tenantId, string email, string passwordHash, string firstName, string lastName, DateTime expiresAt)
    {
        Validate(email, passwordHash);
        var c = new Customer
        {
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            IsDemo = true,
            DemoExpiresAt = expiresAt,
            IsEmailVerified = true,
        };
        return c;
    }

    private static void Validate(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email address is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
    }

    public void VerifyEmail() => IsEmailVerified = true;
    public void ResetPassword(string newPasswordHash) => PasswordHash = newPasswordHash;
    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
