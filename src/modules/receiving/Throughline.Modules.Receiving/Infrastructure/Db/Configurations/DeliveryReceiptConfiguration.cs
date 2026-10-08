using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class DeliveryReceiptConfiguration : IEntityTypeConfiguration<DeliveryReceipt>
{
    // A resubmitted receipt id collides here; matched to tell a duplicate submission from any other unique violation
    public const string PrimaryKeyName = "pk_delivery_receipts";

    // Two receipts for the same owner that compute the same next number at the same time collide here
    public const string OwnerReceiptNumberIndexName = "ix_delivery_receipts_owner_id_receipt_number";

    public void Configure(EntityTypeBuilder<DeliveryReceipt> builder)
    {
        builder.ToTable("delivery_receipts", table =>
            table.HasCheckConstraint(
                "ck_delivery_receipts_trailer_or_container",
                "trailer_number IS NOT NULL OR container_number IS NOT NULL"));

        // The receipt id is client-assigned, so it is never generated here.
        builder.HasKey(r => r.Id).HasName(PrimaryKeyName);
        builder.Property(r => r.Id)
            .HasColumnName("delivery_receipt_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(r => r.OwnerId).HasColumnName("owner_id");
        builder.Property(r => r.ReceiptNumber).HasColumnName("receipt_number").HasMaxLength(15);
        builder.Property(r => r.OperatorId).HasColumnName("operator_id").HasColumnType("uuid");
        builder.Property(r => r.ReceivedOn).HasColumnName("received_on");
        builder.Property(r => r.CreatedOn).HasColumnName("created_on");

        // Receipt numbers are sequential within an owner, not globally.
        builder.HasIndex(r => new { r.OwnerId, r.ReceiptNumber })
            .IsUnique()
            .HasDatabaseName(OwnerReceiptNumberIndexName);

        // Shipment is a value object, so it lives in the receipt's own row rather than a table of its own.
        builder.ComplexProperty(r => r.Shipment, shipment =>
        {
            shipment.Property(s => s.BillOfLading).HasColumnName("bill_of_lading");
            shipment.Property(s => s.ShipperName).HasColumnName("shipper_name");
            shipment.Property(s => s.TrailerNumber).HasColumnName("trailer_number");
            shipment.Property(s => s.ContainerNumber).HasColumnName("container_number");
            shipment.Property(s => s.ShipperReference).HasColumnName("shipper_reference");

            // A snapshot of the carrier as it was when the delivery was received.
            shipment.ComplexProperty(s => s.CarrierScac, carrier =>
            {
                carrier.Property(c => c.ScacCode).HasColumnName("carrier_scac").HasMaxLength(4);
                carrier.Property(c => c.CarrierId).HasColumnName("carrier_id");
                carrier.Property(c => c.CarrierName).HasColumnName("carrier_name");
            });
        });

        // Required: a pallet only exists as part of a receipt. A shadow foreign key is optional by default.
        builder.HasMany(r => r.Pallets)
            .WithOne()
            .HasForeignKey("delivery_receipt_id")
            .IsRequired()
            .HasConstraintName("fk_received_pallets_delivery_receipts_delivery_receipt_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.InvalidPallets)
            .WithOne()
            .HasForeignKey("delivery_receipt_id")
            .IsRequired()
            .HasConstraintName("fk_invalid_pallets_delivery_receipts_delivery_receipt_id")
            .OnDelete(DeleteBehavior.Cascade);

        // The pallet collections are read-only views over backing fields, and are part of the aggregate,
        // so they always load with the receipt.
        builder.Navigation(r => r.Pallets)
            .HasField("_pallets")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.Navigation(r => r.InvalidPallets)
            .HasField("_invalidPallets")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}