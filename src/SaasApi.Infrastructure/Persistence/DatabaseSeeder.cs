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

        // Seed onboarding status for platform tenant
        var hasOnboarding = await db.TenantOnboardingStatuses
            .IgnoreQueryFilters()
            .AnyAsync(s => s.TenantId == platformTenant.Id);
        if (!hasOnboarding)
        {
            var onboarding = TenantOnboardingStatus.Create(platformTenant.Id);
            onboarding.CompleteProfile();
            db.TenantOnboardingStatuses.Add(onboarding);
            await db.SaveChangesAsync();
        }

        // Seed tenant settings for platform tenant
        var hasSettings = await db.TenantSettings
            .IgnoreQueryFilters()
            .AnyAsync(s => s.TenantId == platformTenant.Id);
        if (!hasSettings)
        {
            var settings = TenantSettings.Create(platformTenant.Id);
            db.TenantSettings.Add(settings);
            await db.SaveChangesAsync();
        }

        var adminExists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == platformTenant.Id && u.Role == "super-admin");

        if (!adminExists)
        {
            var passwordHash = passwordHasher.Hash(password);
            var superAdmin = User.Create(platformTenant.Id, email, passwordHash, "super-admin");
            superAdmin.VerifyEmail();
            db.Users.Add(superAdmin);
            await db.SaveChangesAsync();

            var profile = UserProfile.Create(superAdmin.Id, platformTenant.Id, "Super", "Admin");
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();
        }
    }
}
