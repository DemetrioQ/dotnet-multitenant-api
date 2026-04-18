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

    private Customer() { }

    public static Customer Create(Guid tenantId, string email, string passwordHash, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("A valid email address is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        return new Customer
        {
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName
        };
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
