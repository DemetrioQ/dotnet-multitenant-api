namespace SaasApi.Domain.Entities;

/// <summary>
/// Merchant-side role. Customers don't have roles (they're authenticated via sub_type=customer).
/// Stored in the DB as a string via a value converter so the column stays human-readable.
/// </summary>
public enum UserRole
{
    Member = 0,
    Admin = 1,
    SuperAdmin = 2
}

/// <summary>
/// String representations of <see cref="UserRole"/> for use with
/// <c>[Authorize(Roles = ...)]</c> attributes (which only accept strings).
/// Values must match <see cref="UserRoleExtensions.ToDbString"/> exactly.
/// </summary>
public static class RoleNames
{
    public const string Member = "member";
    public const string Admin = "admin";
    public const string SuperAdmin = "super-admin";

    public const string AdminAndAbove = Admin + "," + SuperAdmin;
}

public static class UserRoleExtensions
{
    public static string ToDbString(this UserRole role) => role switch
    {
        UserRole.Member => RoleNames.Member,
        UserRole.Admin => RoleNames.Admin,
        UserRole.SuperAdmin => RoleNames.SuperAdmin,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    public static UserRole ParseRole(string value) => value switch
    {
        RoleNames.Member => UserRole.Member,
        RoleNames.Admin => UserRole.Admin,
        RoleNames.SuperAdmin => UserRole.SuperAdmin,
        _ => throw new ArgumentException($"Unknown role '{value}'.", nameof(value))
    };
}
