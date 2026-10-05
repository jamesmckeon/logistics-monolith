using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Infrastructure.Db.Models;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class CarrierRecordConfiguration : IEntityTypeConfiguration<CarrierRecord>
{
    public void Configure(EntityTypeBuilder<CarrierRecord> builder)
    {
        builder.ToTable("carriers");

        // Carrier ids are assigned by the carrier master, never generated here.
        builder.HasKey(c => c.CarrierId).HasName("pk_carriers");
        builder.Property(c => c.CarrierId).HasColumnName("carrier_id").ValueGeneratedNever();

        // A SCAC identifies exactly one carrier, and deliveries are matched to carriers by it.
        builder.Property(c => c.ScacCode).HasColumnName("scac_code").HasMaxLength(4);
        builder.HasIndex(c => c.ScacCode).IsUnique().HasDatabaseName("ix_carriers_scac_code");

        builder.Property(c => c.CarrierName).HasColumnName("carrier_name");
    }
}
