using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();

        builder.HasIndex(s => s.TenantId).IsUnique();

        builder.Property(s => s.Timezone)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("UTC");

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(s => s.SupportEmail)
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property(s => s.WebsiteUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne<Tenant>()
            .WithOne()
            .HasForeignKey<TenantSettings>(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
