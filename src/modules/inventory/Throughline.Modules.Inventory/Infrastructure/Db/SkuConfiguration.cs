using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    public const string PrimaryKeyName = "pk_skus";

    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");

        builder.HasKey(s => s.Id).HasName(PrimaryKeyName);
        builder.Property(s => s.Id).HasColumnName("sku_id").HasColumnType("uuid");

        builder.Property(s => s.OwnerId).HasColumnName("owner_id");
        builder.Property(s => s.Code).HasColumnName("code").HasMaxLength(50);

        // A SKU code is unique within an owner, not globally (owner-segregated catalog).
        builder.HasIndex(s => new { s.OwnerId, s.Code }).IsUnique();
    }
}