using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Infrastructure.Pricing;

internal sealed class MerchantRateConfiguration : IEntityTypeConfiguration<MerchantRate>
{
    public void Configure(EntityTypeBuilder<MerchantRate> builder)
    {
        builder.ToTable("MerchantRates");
        builder.HasKey(k => k.MerchantId);
        
        builder.Property(z => z.Rate)
            .HasConversion(m => m.Value, v => new Rate(v))
            .HasColumnName("Rate")
            .HasPrecision(18, 2);

    }
}