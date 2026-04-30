using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class OAuthRefreshTokenConfiguration : IEntityTypeConfiguration<OAuthRefreshToken>
{
    public void Configure(EntityTypeBuilder<OAuthRefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.Token).IsRequired().HasMaxLength(128);
        builder.Property(t => t.OAuthClientId).IsRequired();
        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.Scopes).IsRequired().HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.IsRevoked).IsRequired();
        builder.Property(t => t.ReplacedByToken).HasMaxLength(128);

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.OAuthClientId);
        builder.HasIndex(t => t.UserId);
    }
}
