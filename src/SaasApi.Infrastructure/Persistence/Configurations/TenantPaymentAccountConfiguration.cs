using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class TenantPaymentAccountConfiguration : IEntityTypeConfiguration<TenantPaymentAccount>
{
    public void Configure(EntityTypeBuilder<TenantPaymentAccount> builder)
    {
        builder.HasKey(x => x.Id);

        // One payment account per tenant.
        builder.HasIndex(x => x.TenantId).IsUnique();

        // Global lookup by Stripe account id (webhook needs this — no tenant context).
        builder.HasIndex(x => x.AccountId);

        builder.Property(x => x.Provider).IsRequired().HasMaxLength(20);
        builder.Property(x => x.AccountId).IsRequired().HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(new EnumToStringConverter<PaymentAccountStatus>());
    }
}
