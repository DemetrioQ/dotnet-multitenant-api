using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.TenantId).IsRequired();

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(i => i.Token).IsUnique();

        builder.HasIndex(i => new { i.TenantId, i.Email });

        builder.Property(i => i.ExpiresAt).IsRequired();

        builder.Property(i => i.AcceptedAt).IsRequired(false);

        builder.Ignore(i => i.IsExpired);
        builder.Ignore(i => i.IsAccepted);
        builder.Ignore(i => i.IsPending);
    }
}
