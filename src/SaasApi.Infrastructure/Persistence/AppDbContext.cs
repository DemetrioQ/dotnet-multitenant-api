using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Common;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService tenantService)
    : DbContext(options), IAppDbContext
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
    public DbSet<OAuthClient> OAuthClients => Set<OAuthClient>();
    public DbSet<OAuthAuthorizationCode> OAuthAuthorizationCodes => Set<OAuthAuthorizationCode>();
    public DbSet<OAuthRefreshToken> OAuthRefreshTokens => Set<OAuthRefreshToken>();

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

        modelBuilder.Entity<OAuthClient>()
            .HasQueryFilter(c => c.TenantId == tenantService.TenantId);

        modelBuilder.Entity<OAuthAuthorizationCode>()
            .HasQueryFilter(c => c.TenantId == tenantService.TenantId);

        modelBuilder.Entity<OAuthRefreshToken>()
            .HasQueryFilter(t => t.TenantId == tenantService.TenantId);

        // TODO: apply entity configurations from separate IEntityTypeConfiguration<T> classes
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        if (Database.IsOracle())
            ApplyOracleConventions(modelBuilder);
    }

    /// <summary>
    /// Entity configurations are written for SQL Server. This adapts the few places
    /// where Oracle semantics differ, so configurations stay provider-agnostic.
    /// </summary>
    private static void ApplyOracleConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var index in entityType.GetIndexes().ToList())
            {
                if (index.GetFilter() is null)
                    continue;

                if (index.IsUnique)
                {
                    // Oracle has no filtered indexes, and a composite unique index like
                    // (TenantId, Sku) would reject two rows with the same tenant and a NULL
                    // Sku. Drop it from the model; the Oracle InitialCreate migration
                    // recreates it as a function-based unique index with equivalent semantics.
                    entityType.RemoveIndex(index);
                }
                else
                {
                    // Non-unique filtered indexes are only a size optimisation — keep them unfiltered.
                    index.SetFilter(null);
                }
            }

            // Oracle stores '' as NULL, so a required string defaulting to "" can never be
            // satisfied. Allow NULL instead; the domain already treats null and empty alike.
            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(string)))
            {
                if (Equals(property.GetDefaultValue(), string.Empty))
                {
                    property.IsNullable = true;
                    property.SetDefaultValue(null);
                }

                // Unbounded strings are nvarchar(max) on SQL Server but the Oracle provider
                // defaults them to NVARCHAR2(2000). Use NCLOB so long content (e.g. email
                // template HTML) isn't truncated. LOBs can't be indexed, so skip keyed columns.
                if (property.GetMaxLength() is null && !property.IsKey() && !property.IsIndex())
                    property.SetColumnType("NCLOB");
            }
        }
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
