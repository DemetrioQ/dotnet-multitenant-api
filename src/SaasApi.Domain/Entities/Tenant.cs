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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tenant slug cannot be empty.", nameof(slug));

        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new ArgumentException("Slug must be lowercase alphanumeric and may contain hyphens (e.g. 'my-tenant').", nameof(slug));

        return new Tenant { Name = name, Slug = slug };
    }

    public void Deactivate() => IsActive = false;

    public void UpdateName(string name) => Name = name;

}
