using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!; // used in subdomain/header routing
    public bool IsActive { get; private set; } = true;

    private Tenant() { } // EF Core

    public static Tenant Create(string name, string slug)
    {
        // TODO: validate name/slug (no spaces, lowercase, unique enforced at DB level)
        return new Tenant { Name = name, Slug = slug };
    }
}
