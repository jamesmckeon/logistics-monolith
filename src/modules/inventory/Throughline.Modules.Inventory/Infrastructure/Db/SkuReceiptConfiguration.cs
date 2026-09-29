using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Receiving;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class SkuReceiptConfiguration : IEntityTypeConfiguration<InventoryPallet>
{
    public const string PrimaryKeyName = "pk_sku_receipts";

    public void Configure(EntityTypeBuilder<InventoryPallet> builder)
    {
        builder.ToTable("sku_receipts");

        builder.HasKey(r => r.Id).HasName(PrimaryKeyName);
        builder.Property(r => r.Id).HasColumnName("sku_receipt_id").HasColumnType("uuid");

        builder.Property(r => r.SkuId).HasColumnName("sku_id").HasColumnType("uuid");
        builder.Property(r => r.QuantityReceived).HasColumnName("quantity_received");
        builder.Property(r => r.QuantityAllocated).HasColumnName("quantity_allocated");
        builder.Property(r => r.QuantityAvailable).HasColumnName("quantity_available");

        builder.Property(r => r.ReceivedOn)
            .HasColumnName("received_on")
            .HasConversion(
                v => v.Value,
                v => new AppDateTime(v));

        builder.Property(r => r.LastUpdated)
            .HasColumnName("last_updated")
            .HasConversion<AppDateTimeValueConverter>();

        // Referential integrity to the SKU catalog (no orphaned receipts), by identity only —
        // no navigation, so the receipt stays decoupled from the Sku aggregate.
        builder.HasOne<Sku>()
            .WithMany()
            .HasForeignKey(r => r.SkuId)
            .HasConstraintName("fk_sku_receipts_skus_sku_id");

        builder.HasIndex(i => i.ReceivedOn, "ix_skureceipts_receivedon");
    }
}