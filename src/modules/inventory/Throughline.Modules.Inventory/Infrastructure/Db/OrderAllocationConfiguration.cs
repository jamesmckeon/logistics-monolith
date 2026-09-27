using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class OrderAllocationConfiguration : IEntityTypeConfiguration<OrderAllocation>
{
    // SaveConfirmedOrder matches on this to distinguish a duplicate order (safe to swallow)
    // from any other unique violation (a real error worth rethrowing).
    public const string PrimaryKeyName = "pk_order_allocations";

    public void Configure(EntityTypeBuilder<OrderAllocation> builder)
    {
        builder.ToTable("order_allocations");

        // OrderId is a globally-unique UUIDv7, so it is the identity; owner is a scope attribute.
        builder.HasKey(o => o.Id).HasName(PrimaryKeyName);
        builder.Property(o => o.Id).HasColumnName("order_id").HasColumnType("uuid");

        builder.Property(o => o.OwnerId).HasColumnName("owner_id");
        builder.HasIndex(o => o.OwnerId);

        builder.Property(o => o.LastUpdated)
            .HasColumnName("last_updated")
            .HasConversion(
                v => v!.Value,
                v => new AppDateTime(v));

        // Optimistic concurrency via PostgreSQL's xmin system column
        builder.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.OwnsMany(o => o.OrderLines, line =>
        {
            line.ToTable("orderline_allocations");
            line.WithOwner().HasForeignKey("order_id");
            line.Property<Guid>("order_id").HasColumnType("uuid");

            // EntityId is a globally-unique UUIDv7 assigned by the domain, so it is the key on its own.
            line.HasKey(l => l.Id).HasName("pk_orderline_allocations");
            line.Property(l => l.Id)
                .HasColumnName("orderline_allocation_id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

            line.Property(l => l.SkuId).HasColumnName("sku_id").HasColumnType("uuid");
            line.Property(l => l.QuantityRequested).HasColumnName("quantity_requested");
            line.Property(l => l.LastUpdated)
                .HasColumnName("last_updated")
                .HasConversion<AppDateTimeValueConverter>();

            line.OwnsMany(l => l.ReceiptAllocations, receipt =>
            {
                receipt.ToTable("receipt_allocations");
                receipt.WithOwner().HasForeignKey("orderline_allocation_id");
                receipt.Property("orderline_allocation_id").HasColumnType("uuid").IsRequired();

                // ReceiptAllocation has no identity of its own, and a line may draw on the same receipt more
                // than once, so a surrogate key identifies each row rather than (line, receipt).
                receipt.Property<Guid>("receipt_allocation_id").HasColumnType("uuid").ValueGeneratedOnAdd();
                receipt.HasKey("receipt_allocation_id").HasName("pk_receipt_allocations");

                receipt.Property(r => r.SkuReceiptId).HasColumnName("sku_receipt_id").HasColumnType("uuid");
                receipt.Property(r => r.QuantityAllocated).HasColumnName("quantity_allocated");
                receipt.Property(r => r.AllocatedOn)
                    .HasColumnName("allocated_on")
                    .HasConversion<AppDateTimeValueConverter>();

                // Reverse lookup: which order lines hold stock from a given receipt.
                receipt.HasIndex(r => r.SkuReceiptId, "ix_receipt_allocations_sku_receipt_id");
            });

            // ReceiptAllocations is a read-only view over the _allocations backing field.
            line.Navigation(l => l.ReceiptAllocations)
                .HasField("_allocations")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // OrderLines is an encapsulated read-only view over the _orderLines backing field.
        builder.Navigation(o => o.OrderLines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(i => i.UnallocatedLines);
    }
}