using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Number).IsRequired().HasMaxLength(40);
        builder.HasIndex(o => new { o.TenantId, o.Number }).IsUnique();

        builder.Property(o => o.Status).IsRequired().HasConversion<int>();
        builder.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Total).HasColumnType("decimal(18,2)");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.CustomerId);

        builder.Property(o => o.ShippingLine1).IsRequired().HasMaxLength(200);
        builder.Property(o => o.ShippingLine2).HasMaxLength(200);
        builder.Property(o => o.ShippingCity).IsRequired().HasMaxLength(100);
        builder.Property(o => o.ShippingRegion).HasMaxLength(100);
        builder.Property(o => o.ShippingPostalCode).IsRequired().HasMaxLength(20);
        builder.Property(o => o.ShippingCountry).IsRequired().HasMaxLength(2);

        builder.Property(o => o.BillingLine1).IsRequired().HasMaxLength(200);
        builder.Property(o => o.BillingLine2).HasMaxLength(200);
        builder.Property(o => o.BillingCity).IsRequired().HasMaxLength(100);
        builder.Property(o => o.BillingRegion).HasMaxLength(100);
        builder.Property(o => o.BillingPostalCode).IsRequired().HasMaxLength(20);
        builder.Property(o => o.BillingCountry).IsRequired().HasMaxLength(2);

        builder.Property(o => o.PaymentProvider).HasMaxLength(20);
        builder.Property(o => o.PaymentSessionId).HasMaxLength(200);

        // Used by the webhook to look up an order by Stripe session id.
        builder.HasIndex(o => new { o.TenantId, o.PaymentSessionId })
            .IsUnique()
            .HasFilter("[PaymentSessionId] IS NOT NULL");
    }
}
