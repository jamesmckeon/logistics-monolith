using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Infrastructure.Pricing;

internal sealed class MerchantPickFeeConfiguration : IEntityTypeConfiguration<MerchantPickFee>
{
    public void Configure(EntityTypeBuilder<MerchantPickFee> builder)
    {
        builder.ToTable("PickFees");

        builder.Property(f => f.SkuCode)
            .HasConversion(sku => sku.Value, value => new SkuCode(value))
            .HasColumnName("SkuCode")
            .HasMaxLength(16);

        builder.HasKey(f => new { f.MerchantId, f.SkuCode });

        builder.Property(f => f.PickFee)
            .HasConversion(rate => rate.Value, value => new Rate(value))
            .HasColumnName("PickFee")
            .HasPrecision(18, 4);
    }
}