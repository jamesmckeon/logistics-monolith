using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class PalletConfiguration : IEntityTypeConfiguration<Pallet>
{
    public void Configure(EntityTypeBuilder<Pallet> builder)
    {
        builder.ToTable("received_pallets", table =>
            table.HasCheckConstraint("ck_received_pallets_quantity", "quantity > 0"));

        builder.HasKey(p => p.Id).HasName("pk_received_pallets");
        builder.Property(p => p.Id)
            .HasColumnName("received_pallet_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(p => p.LicensePlateNumber).HasColumnName("license_plate_number");
        builder.Property(p => p.Quantity).HasColumnName("quantity");
        builder.Property(p => p.ExpiresOn).HasColumnName("expires_on");
        builder.Property(p => p.LotNumber).HasColumnName("lot_number");

        // A snapshot of the SKU as it was when the pallet was received; the pallet's owner is the SKU's owner.
        builder.ComplexProperty(p => p.OwnerSku, sku =>
        {
            sku.Property(s => s.Id).HasColumnName("sku_id").HasColumnType("uuid");
            sku.Property(s => s.OwnerId).HasColumnName("owner_id");
            sku.Property(s => s.SkuCode).HasColumnName("sku_code");
            sku.Property(s => s.IsLotTracked).HasColumnName("sku_is_lot_tracked");
            sku.Property(s => s.IsExpirationTracked).HasColumnName("sku_is_expiration_tracked");
        });

        builder.HasOne(p => p.Location)
            .WithMany()
            .HasForeignKey("location_id")
            .IsRequired()
            .HasConstraintName("fk_received_pallets_receiving_locations_location_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Hold reasons are keyed by owner + code, so the foreign key is too.
        builder.HasOne(p => p.HoldReason)
            .WithMany()
            .HasForeignKey("hold_reason_owner_id", "hold_reason_code")
            .IsRequired(false)
            .HasConstraintName("fk_received_pallets_hold_reasons_hold_reason")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(p => p.Location).AutoInclude();
        builder.Navigation(p => p.HoldReason).AutoInclude();

        builder.Ignore(p => p.IsHeld);
        builder.Ignore(p => p.IsAllocatable);
    }
}
