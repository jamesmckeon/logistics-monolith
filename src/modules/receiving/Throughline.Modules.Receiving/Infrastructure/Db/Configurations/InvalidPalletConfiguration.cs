using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class InvalidPalletConfiguration : IEntityTypeConfiguration<InvalidPallet>
{
    public void Configure(EntityTypeBuilder<InvalidPallet> builder)
    {
        builder.ToTable("invalid_pallets", table =>
        {
            table.HasCheckConstraint("ck_invalid_pallets_quantity", "quantity > 0");
            table.HasCheckConstraint("ck_invalid_pallets_exceptions", "cardinality(exceptions) > 0");
        });

        builder.HasKey(p => p.Id).HasName("pk_invalid_pallets");
        builder.Property(p => p.Id)
            .HasColumnName("invalid_pallet_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(p => p.OwnerId).HasColumnName("owner_id");
        builder.Property(p => p.SkuCode).HasColumnName("sku_code");
        builder.Property(p => p.LicensePlateNumber).HasColumnName("license_plate_number");
        builder.Property(p => p.Quantity).HasColumnName("quantity");
        builder.Property(p => p.RequestLocationId).HasColumnName("request_location_id");
        builder.Property(p => p.LotNumber).HasColumnName("lot_number");
        builder.Property(p => p.ExpiresOn).HasColumnName("expires_on");

        // Stored by name so the column stays readable and enum reordering can't change its meaning.
        builder.PrimitiveCollection(p => p.Exceptions)
            .HasColumnName("exceptions")
            .ElementType()
            .HasConversion<string>();

        builder.HasOne(p => p.Location)
            .WithMany()
            .HasForeignKey("location_id")
            .IsRequired()
            .HasConstraintName("fk_invalid_pallets_receiving_locations_location_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(p => p.Location).AutoInclude();
    }
}
