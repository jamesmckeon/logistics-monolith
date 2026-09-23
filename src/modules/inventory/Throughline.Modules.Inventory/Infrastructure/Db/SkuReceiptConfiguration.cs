using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class SkuReceiptConfiguration : IEntityTypeConfiguration<SkuReceipt>
{
    public const string PrimaryKeyName = "pk_sku_receipts";

    public void Configure(EntityTypeBuilder<SkuReceipt> builder)
    {
        builder.ToTable("sku_receipts");

        builder.HasKey(r => r.Id).HasName(PrimaryKeyName);
        builder.Property(r => r.Id).HasColumnName("sku_receipt_id").HasColumnType("uuid");

        builder.Property(r => r.SkuId).HasColumnName("sku_id").HasColumnType("uuid");
        builder.Property(r => r.OwnerId).HasColumnName("owner_id");
        builder.Property(r => r.QuantityReceived).HasColumnName("quantity_received");

        builder.Property(r => r.ReceivedOn)
            .HasColumnName("received_on")
            .HasConversion(
                v => v.Value,
                v => new AppDateTime(v));

        // Referential integrity to the SKU catalog (no orphaned receipts), by identity only —
        // no navigation, so the receipt stays decoupled from the Sku aggregate.
        builder.HasOne<Sku>()
            .WithMany()
            .HasForeignKey(r => r.SkuId)
            .HasConstraintName("fk_sku_receipts_skus_sku_id");

        // The hot allocation filter: available receipts for an owner's SKU, oldest first (FEFO).
        builder.HasIndex(r => new { r.OwnerId, r.SkuId });

        builder.Ignore(r => r.QuantityAvailable);

        builder.OwnsMany(r => r.Allocations, a =>
        {
            a.ToTable("receipt_allocations");
            a.WithOwner().HasForeignKey("sku_receipt_id");
            a.Property<Guid>("sku_receipt_id").HasColumnType("uuid");
            a.Property(x => x.OrderId).HasColumnName("order_id").HasColumnType("uuid");
            a.Property(x => x.QuantityAllocated).HasColumnName("quantity_allocated");
            a.Property(x => x.AllocatedOn)
                .HasColumnName("allocated_on")
                .HasConversion(
                    v => v.Value,
                    v => new AppDateTime(v));
        });

        // Allocations is an encapsulated read-only view over the _allocations backing field.
        builder.Navigation(r => r.Allocations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}