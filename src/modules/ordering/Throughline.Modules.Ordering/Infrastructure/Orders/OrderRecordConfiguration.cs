using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrderRecordConfiguration : IEntityTypeConfiguration<OrderRecord>
{
    public const string OwnerReferenceIndexName = "IX_Orders_UniqueOwnerReference";

    public void Configure(EntityTypeBuilder<OrderRecord> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.OrderId);

        builder.Property(o => o.OrderId)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(o => o.PurchaseOrderNumber)
            .HasMaxLength(50);

        builder.Property(o => o.ReferenceNumber)
            .HasMaxLength(50);

        builder.Property(o => o.StreetAddressOne)
            .HasMaxLength(150);

        builder.Property(o => o.StreetAddressTwo)
            .HasMaxLength(150);

        builder.Property(o => o.City)
            .HasMaxLength(50);

        builder.Property(o => o.State)
            .HasMaxLength(2)
            .IsFixedLength();

        builder.Property(o => o.Zipcode)
            .HasMaxLength(10);

        builder.HasMany(o => o.OrderLines)
            .WithOne()
            .HasForeignKey(l => l.OrderId);

        // Enforces "one order per owner + reference" at the database. The explicit database name
        // is matched by OrdersRepository's duplicate-insert catch, so it must stay stable and is
        // pinned here (not left to the snake_case naming convention, which rewrites derived names).
        builder.HasIndex(o => new { o.OwnerId, o.ReferenceNumber })
            .IsUnique()
            .HasDatabaseName(OwnerReferenceIndexName);
    }
}