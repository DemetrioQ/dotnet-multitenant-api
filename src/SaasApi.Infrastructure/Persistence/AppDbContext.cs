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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<TenantOnboardingStatus> TenantOnboardingStatuses => Set<TenantOnboardingStatus>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerRefreshToken> CustomerRefreshTokens => Set<CustomerRefreshToken>();
    public DbSet<CustomerEmailVerificationToken> CustomerEmailVerificationTokens => Set<CustomerEmailVerificationToken>();
    public DbSet<CustomerPasswordResetToken> CustomerPasswordResetTokens => Set<CustomerPasswordResetToken>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<TenantEmailTemplate> TenantEmailTemplates => Set<TenantEmailTemplate>();
    public DbSet<TenantPaymentAccount> TenantPaymentAccounts => Set<TenantPaymentAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == tenantService.TenantId);

        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(r => r.TenantId == tenantService.TenantId);

        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => p.TenantId == tenantService.TenantId);

        modelBuilder.Entity<EmailVerificationToken>()
            .HasQueryFilter(e => e.TenantId == tenantService.TenantId);

        modelBuilder.Entity<PasswordResetToken>()
            .HasQueryFilter(p => p.TenantId == tenantService.TenantId);

        modelBuilder.Entity<TenantOnboardingStatus>()
            .HasQueryFilter(s => s.TenantId == tenantService.TenantId);

        modelBuilder.Entity<TenantSettings>()
            .HasQueryFilter(s => s.TenantId == tenantService.TenantId);

        modelBuilder.Entity<Invitation>()
            .HasQueryFilter(i => i.TenantId == tenantService.TenantId);

        modelBuilder.Entity<UserProfile>()
            .HasQueryFilter(p => p.TenantId == tenantService.TenantId);

        modelBuilder.Entity<AuditLogEntry>()
            .HasQueryFilter(a => a.TenantId == tenantService.TenantId);

        modelBuilder.Entity<Customer>()
            .HasQueryFilter(c => c.TenantId == tenantService.TenantId);

        modelBuilder.Entity<CustomerRefreshToken>()
            .HasQueryFilter(r => r.TenantId == tenantService.TenantId);

        modelBuilder.Entity<CustomerEmailVerificationToken>()
            .HasQueryFilter(t => t.TenantId == tenantService.TenantId);

        modelBuilder.Entity<CustomerPasswordResetToken>()
            .HasQueryFilter(t => t.TenantId == tenantService.TenantId);

        modelBuilder.Entity<Cart>()
            .HasQueryFilter(c => c.TenantId == tenantService.TenantId);

        modelBuilder.Entity<CartItem>()
            .HasQueryFilter(i => i.TenantId == tenantService.TenantId);

        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => o.TenantId == tenantService.TenantId);

        modelBuilder.Entity<OrderItem>()
            .HasQueryFilter(i => i.TenantId == tenantService.TenantId);

        modelBuilder.Entity<CustomerAddress>()
            .HasQueryFilter(a => a.TenantId == tenantService.TenantId);

        modelBuilder.Entity<TenantEmailTemplate>()
            .HasQueryFilter(t => t.TenantId == tenantService.TenantId);

        modelBuilder.Entity<TenantPaymentAccount>()
            .HasQueryFilter(a => a.TenantId == tenantService.TenantId);

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
