using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Throughline.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderRecordConfiguration : IEntityTypeConfiguration<OrderRecord>
{
    // Pinned so the DB constraint name is a stable contract, not an EF-generated default.
    // SaveConfirmedOrder matches on this to distinguish a duplicate order (safe to swallow)
    // from any other unique violation (a real error worth rethrowing).
    public const string PrimaryKeyName = "pk_orders";

    public void Configure(EntityTypeBuilder<OrderRecord> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => new { o.OwnerId, o.OrderId }).HasName(PrimaryKeyName);

        builder.Property(o => o.OwnerId).HasColumnName("owner_id");
        builder.Property(o => o.OrderId).HasColumnName("order_id").HasColumnType("uuid");

        builder.HasMany(o => o.OrderLines)
            .WithOne()
            .HasForeignKey(l => new { l.OwnerId, l.OrderId });
    }
}