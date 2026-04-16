using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private const string PlatformSlug = "platform";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, IConfiguration config)
    {
        var email = config["SuperAdmin:Email"] ?? "admin@platform.com";
        var password = config["SuperAdmin:Password"]
            ?? throw new InvalidOperationException("SuperAdmin:Password is not configured.");

        var platformTenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == PlatformSlug);

        if (platformTenant is null)
        {
            platformTenant = Tenant.Create("Platform", PlatformSlug);
            db.Tenants.Add(platformTenant);
            await db.SaveChangesAsync();
        }

        var adminExists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == platformTenant.Id && u.Role == "super-admin");

        if (!adminExists)
        {
            var passwordHash = passwordHasher.Hash(password);
            var superAdmin = User.Create(platformTenant.Id, email, passwordHash, "super-admin");
            db.Users.Add(superAdmin);
            await db.SaveChangesAsync();
        }
    }
}
