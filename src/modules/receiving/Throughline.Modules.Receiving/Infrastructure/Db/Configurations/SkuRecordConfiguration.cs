using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Infrastructure.Db.Models;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class SkuRecordConfiguration : IEntityTypeConfiguration<SkuRecord>
{
    public void Configure(EntityTypeBuilder<SkuRecord> builder)
    {
        builder.ToTable("skus");

        // SKU ids are assigned by the SKU master, never generated here.
        builder.HasKey(s => s.SkuId).HasName("pk_skus");
        builder.Property(s => s.SkuId).HasColumnName("sku_id").HasColumnType("uuid").ValueGeneratedNever();

        builder.Property(s => s.OwnerId).HasColumnName("owner_id");
        builder.Property(s => s.SkuCode).HasColumnName("sku_code");
        builder.Property(s => s.IsLotTracked).HasColumnName("is_lot_tracked");
        builder.Property(s => s.IsExpirationTracked).HasColumnName("is_expiration_tracked");

        // A SKU code is unique within an owner, not globally (owner-segregated catalog); this index also serves
        // the owner + code lookup when a delivery is received.
        builder.HasIndex(s => new { s.OwnerId, s.SkuCode })
            .IsUnique()
            .HasDatabaseName("ix_skus_owner_id_sku_code");
    }
}
