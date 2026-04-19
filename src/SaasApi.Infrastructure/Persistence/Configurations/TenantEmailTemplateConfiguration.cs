using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class TenantEmailTemplateConfiguration : IEntityTypeConfiguration<TenantEmailTemplate>
{
    public void Configure(EntityTypeBuilder<TenantEmailTemplate> builder)
    {
        builder.HasKey(t => t.Id);

        // Only one override row per (tenant, type). Absence means "use default".
        builder.HasIndex(t => new { t.TenantId, t.Type }).IsUnique();

        builder.Property(t => t.Type)
            .IsRequired()
            .HasMaxLength(40)
            .HasConversion(new EnumToStringConverter<EmailTemplateType>());

        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.BodyHtml)
            .IsRequired();

        builder.Property(t => t.Enabled).IsRequired();
    }
}
