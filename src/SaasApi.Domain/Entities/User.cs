using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = "member"; // e.g. admin, member

    private User() { } // EF Core

    public static User Create(Guid tenantId, string email, string passwordHash, string role = "member")
    {
        // TODO: validate email format, hash must not be plain text
        return new User { TenantId = tenantId, Email = email, PasswordHash = passwordHash, Role = role };
    }
}
