using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrderRecordConfiguration : IEntityTypeConfiguration<OrderRecord>
{
    public void Configure(EntityTypeBuilder<OrderRecord> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.OrderId);

        builder.Property(o => o.OrderId)
            .HasColumnName("OrderId")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(o => o.PurchaseOrderNumber)
            .HasMaxLength(50);

        builder.Property(o => o.ReferenceNumber)
            .HasMaxLength(50);

        builder.Property(o => o.StreetAddressOne)
            .HasColumnName("StreetAddressOne")
            .HasMaxLength(150);

        builder.Property(o => o.StreetAddressTwo)
            .HasColumnName("StreetAddressTwo")
            .HasMaxLength(150);

        builder.Property(o => o.City)
            .HasColumnName("City")
            .HasMaxLength(50);

        builder.Property(o => o.State)
            .HasColumnName("State")
            .HasMaxLength(2)
            .IsFixedLength();

        builder.Property(o => o.Zipcode)
            .HasColumnName("Zipcode")
            .HasMaxLength(10);

        builder.HasMany(o => o.OrderLines)
            .WithOne()
            .HasForeignKey(l => l.OrderId);

        // Backs OrderExistsFor and enforces "one order per merchant + reference" at the database.
        builder.HasIndex(o => new { o.MerchantId, o.ReferenceNumber })
            .IsUnique();
    }
}
