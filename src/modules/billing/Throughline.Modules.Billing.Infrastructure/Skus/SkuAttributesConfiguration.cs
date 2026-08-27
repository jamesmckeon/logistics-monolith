using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Billing.Domain.Skus;

namespace Throughline.Modules.Billing.Infrastructure.Skus;

internal sealed class SkuAttributesConfiguration : IEntityTypeConfiguration<SkuAttributes>
{
    public void Configure(EntityTypeBuilder<SkuAttributes> builder)
    {
        builder.ToTable("SkuAttributes");
        builder.HasKey(k => k.SkuCode);

        builder.Property(z => z.SkuCode)
            .HasConversion(m => m.Value, v => new SkuCode(v))
            .HasMaxLength(16);
    }
}