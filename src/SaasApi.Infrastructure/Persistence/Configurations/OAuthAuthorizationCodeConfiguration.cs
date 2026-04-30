using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class OAuthAuthorizationCodeConfiguration : IEntityTypeConfiguration<OAuthAuthorizationCode>
{
    public void Configure(EntityTypeBuilder<OAuthAuthorizationCode> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(64);
        builder.Property(c => c.OAuthClientId).IsRequired();
        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.RedirectUri).IsRequired().HasMaxLength(500);
        builder.Property(c => c.CodeChallenge).IsRequired().HasMaxLength(128);
        builder.Property(c => c.CodeChallengeMethod).IsRequired().HasMaxLength(16);
        builder.Property(c => c.Scopes).IsRequired().HasMaxLength(500).HasDefaultValue(string.Empty);
        builder.Property(c => c.ExpiresAt).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.ExpiresAt);
    }
}
