using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Skus;

namespace Throughline.Modules.Ordering.Infrastructure.Skus;

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