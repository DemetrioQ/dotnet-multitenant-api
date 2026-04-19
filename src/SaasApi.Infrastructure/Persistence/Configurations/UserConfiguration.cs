using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(100);

        // Store enum as its ToDbString() value so the column stays human-readable
        // ("member", "admin", "super-admin") — matches the existing schema.
        // No DB-level default value: User.Create always sets Role explicitly, and the
        // entity field initializer defaults to UserRole.Member at object construction.
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                toDb => toDb.ToDbString(),
                fromDb => UserRoleExtensions.ParseRole(fromDb));

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
