using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class CustomerPasswordResetTokenConfiguration : IEntityTypeConfiguration<CustomerPasswordResetToken>
{
    public void Configure(EntityTypeBuilder<CustomerPasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).IsRequired().HasMaxLength(64);
        builder.HasIndex(t => t.Token).IsUnique();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.ExpiresAt).IsRequired();
    }
}
