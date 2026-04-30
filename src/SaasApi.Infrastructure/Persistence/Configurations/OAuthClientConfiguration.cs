using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class OAuthClientConfiguration : IEntityTypeConfiguration<OAuthClient>
{
    public void Configure(EntityTypeBuilder<OAuthClient> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.ClientId).IsRequired().HasMaxLength(64);
        // Nullable: public clients have no secret (PKCE-only).
        builder.Property(c => c.ClientSecretHash).HasMaxLength(200);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ClientType).IsRequired().HasConversion<int>();
        builder.Property(c => c.Scopes).IsRequired().HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(c => c.RedirectUris).IsRequired().HasMaxLength(2000).HasDefaultValue(string.Empty);
        builder.Property(c => c.IsRevoked).IsRequired();

        builder.HasIndex(c => c.ClientId).IsUnique();
        builder.HasIndex(c => c.TenantId);
    }
}
