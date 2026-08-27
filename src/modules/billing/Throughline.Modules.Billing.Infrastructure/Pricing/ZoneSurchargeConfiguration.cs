using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Billing.Domain.Pricing;

namespace Throughline.Modules.Billing.Infrastructure.Pricing;

internal sealed class ZoneSurchargeConfiguration : IEntityTypeConfiguration<ZoneSurcharge>
{
    public void Configure(EntityTypeBuilder<ZoneSurcharge> builder)
    {
        builder.ToTable("ZoneSurcharges");

        builder.Property<long>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");

        builder.ComplexProperty(z => z.PostalZone, pz =>
        {
            pz.Property(p => p.StartCode)
                .HasConversion(c => c.Value, v => new PostalCode(v))
                .HasColumnName("StartZip")
                .HasMaxLength(10);
            pz.Property(p => p.EndCode)
                .HasConversion(c => c.Value, v => new PostalCode(v))
                .HasColumnName("EndZip")
                .HasMaxLength(10);
        });

        builder.Property(z => z.Surcharge)
            .HasConversion(m => m.Value, v => new Money(v))
            .HasColumnName("Surcharge")
            .HasPrecision(18, 2);

        builder.HasIndex(z => new { z.MerchantId, z.PostalZone.StartCode, z.PostalZone.EndCode })
            .IsUnique();
    }
}