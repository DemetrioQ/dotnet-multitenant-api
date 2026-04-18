using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.HasIndex(c => new { c.TenantId, c.Email }).IsUnique();

        builder.Property(c => c.PasswordHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.IsEmailVerified).IsRequired().HasDefaultValue(false);
    }
}
