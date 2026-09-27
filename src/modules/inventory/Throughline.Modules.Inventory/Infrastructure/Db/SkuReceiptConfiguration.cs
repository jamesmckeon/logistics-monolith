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
        builder.Property(r => r.QuantityReceived).HasColumnName("quantity_received");
        builder.Property(r => r.QuantityAllocated).HasColumnName("quantity_allocated");

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

        // The domain derives QuantityAvailable in memory; the database stores it as a generated column so
        // queries can filter on it and a locked row's re-check sees the current value.
        builder.Ignore(r => r.QuantityAvailable);
        builder.Property<int>("quantity_available")
            .HasColumnName("quantity_available")
            .HasComputedColumnSql("quantity_received - quantity_allocated", stored: true);

        builder.HasIndex(i => i.ReceivedOn, "ix_skureceipts_receivedon");
    }
}