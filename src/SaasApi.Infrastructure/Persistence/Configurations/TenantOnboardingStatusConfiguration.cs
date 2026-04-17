using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class TenantOnboardingStatusConfiguration : IEntityTypeConfiguration<TenantOnboardingStatus>
{
    public void Configure(EntityTypeBuilder<TenantOnboardingStatus> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).IsRequired();

        builder.HasIndex(t => t.TenantId).IsUnique();

        builder.Property(t => t.ProfileCompleted).IsRequired();
        builder.Property(t => t.FirstProductCreated).IsRequired();

        builder.Ignore(t => t.IsComplete);

        builder.HasOne<Tenant>()
            .WithOne()
            .HasForeignKey<TenantOnboardingStatus>(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
