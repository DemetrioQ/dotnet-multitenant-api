using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.UserEmail).IsRequired().HasMaxLength(320);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId);
        builder.Property(a => a.Details).HasMaxLength(2000);

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.CreatedAt);
    }
}
