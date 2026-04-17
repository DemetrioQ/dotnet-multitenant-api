using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.TenantId).IsRequired();

        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired(false);
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired(false);
        builder.Property(p => p.AvatarUrl).HasMaxLength(500).IsRequired(false);
        builder.Property(p => p.Bio).HasMaxLength(500).IsRequired(false);

        builder.Ignore(p => p.IsComplete);

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
