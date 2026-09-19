using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Throughline.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderLineRecordConfiguration : IEntityTypeConfiguration<OrderLineRecord>
{
    public void Configure(EntityTypeBuilder<OrderLineRecord> builder)
    {
        builder.ToTable("order_lines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.OwnerId).HasColumnName("owner_id");
        builder.Property(l => l.OrderId).HasColumnName("order_id").HasColumnType("uuid");
        builder.Property(l => l.SkuCode).HasColumnName("sku_code").HasMaxLength(50);
        builder.Property(l => l.QuantityRequested).HasColumnName("quantity_requested");
    }
}
