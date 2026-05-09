using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Demo.Commands.ProvisionDemoTenant;

public class ProvisionDemoTenantHandler(
    IRepository<Tenant> tenantRepo,
    IRepository<User> userRepo,
    IRepository<UserProfile> profileRepo,
    IRepository<TenantOnboardingStatus> onboardingRepo,
    IRepository<TenantSettings> settingsRepo,
    IRepository<Product> productRepo,
    IRepository<RefreshToken> refreshTokenRepo,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<ProvisionDemoTenantCommand, ProvisionDemoTenantResult>
{
    private static readonly TimeSpan TenantLifetime = TimeSpan.FromHours(24);

    public async Task<ProvisionDemoTenantResult> Handle(ProvisionDemoTenantCommand request, CancellationToken ct)
    {
        var slug = await GenerateUniqueSlugAsync(ct);
        var name = $"Demo Store {slug[5..].ToUpperInvariant()}";
        var expiresAt = DateTime.UtcNow.Add(TenantLifetime);

        var tenant = Tenant.CreateDemo(name, slug, expiresAt);
        await tenantRepo.AddAsync(tenant, ct);

        // Random throwaway password — visitor can't log in directly anyway, JWT is returned.
        var passwordHash = passwordHasher.Hash(Guid.NewGuid().ToString("N"));
        var user = User.Create(tenant.Id, $"demo@{slug}.local", passwordHash, UserRole.Admin);
        user.VerifyEmail();
        await userRepo.AddAsync(user, ct);

        var profile = UserProfile.Create(user.Id, tenant.Id, "Demo", "Merchant");
        await profileRepo.AddAsync(profile, ct);

        // Mark onboarding complete so dashboard skips the wizard.
        var onboarding = TenantOnboardingStatus.Create(tenant.Id);
        onboarding.CompleteProfile();
        onboarding.CompleteFirstProduct();
        await onboardingRepo.AddAsync(onboarding, ct);

        var settings = TenantSettings.Create(tenant.Id);
        settings.Update($"support@{slug}.local", null);
        await settingsRepo.AddAsync(settings, ct);

        foreach (var seed in DemoProductCatalog)
        {
            var product = Product.Create(
                tenant.Id, seed.Name, seed.Slug, seed.Description, seed.Price, seed.Stock);
            await productRepo.AddAsync(product, ct);
        }

        var refreshToken = RefreshToken.Create(user.TenantId, user.Id, Guid.NewGuid());
        await refreshTokenRepo.AddAsync(refreshToken, ct);

        await tenantRepo.SaveChangesAsync(ct);

        var jwt = jwtTokenService.GenerateToken(user);

        return new ProvisionDemoTenantResult(
            jwt, refreshToken.Token, refreshToken.ExpiresAt,
            tenant.Slug, tenant.Name, expiresAt);
    }

    private async Task<string> GenerateUniqueSlugAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"demo-{Guid.NewGuid().ToString("N")[..8]}";
            var existing = await tenantRepo.FindGlobalAsync(t => t.Slug == candidate, ct);
            if (!existing.Any()) return candidate;
        }
        throw new InvalidOperationException("Failed to generate a unique demo tenant slug.");
    }

    private record DemoProduct(string Name, string Slug, string Description, decimal Price, int Stock);

    private static readonly DemoProduct[] DemoProductCatalog =
    [
        new("Linen Crew Tee", "linen-crew-tee", "A breathable everyday tee in 100% European linen.", 38.00m, 24),
        new("Canvas Tote", "canvas-tote", "Heavy-duty 16oz canvas tote with reinforced straps.", 24.00m, 50),
        new("Ceramic Pour-Over", "ceramic-pour-over", "Hand-thrown stoneware dripper sized for a single mug.", 32.00m, 12),
        new("Merino Beanie", "merino-beanie", "Lightweight ribbed beanie in fine merino wool.", 28.00m, 30),
        new("Walnut Cutting Board", "walnut-cutting-board", "Edge-grain walnut, oiled finish, food-safe.", 65.00m, 8),
        new("Brass Desk Lamp", "brass-desk-lamp", "Articulating brass desk lamp with a warm-white LED bulb.", 120.00m, 6),
    ];
}
