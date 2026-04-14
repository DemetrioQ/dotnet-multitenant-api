using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Common;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService tenantService)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter — every query on ITenantEntity is automatically scoped to the current tenant.
        // TODO: add this filter for every entity that implements ITenantEntity
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == tenantService.TenantId);

        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(r => r.TenantId == tenantService.TenantId);

        // TODO: apply entity configurations from separate IEntityTypeConfiguration<T> classes
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Auto-set UpdatedAt on modified entities
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(ct);
    }
}
