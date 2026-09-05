using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrderLineRecordConfiguration : IEntityTypeConfiguration<OrderLineRecord>
{
    public void Configure(EntityTypeBuilder<OrderLineRecord> builder)
    {
        builder.ToTable("order_lines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd();

        builder.Property(l => l.SkuCode)
            .HasMaxLength(50);
    }
}