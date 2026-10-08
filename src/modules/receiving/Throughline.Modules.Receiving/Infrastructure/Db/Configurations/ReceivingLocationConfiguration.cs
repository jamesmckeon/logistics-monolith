using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class ReceivingLocationConfiguration : IEntityTypeConfiguration<ReceivingLocation>
{
    public void Configure(EntityTypeBuilder<ReceivingLocation> builder)
    {
        builder.ToTable("receiving_locations");

        // Location ids are assigned by the warehouse, never generated here.
        builder.HasKey(l => l.Id).HasName("pk_receiving_locations");
        builder.Property(l => l.Id).HasColumnName("location_id").ValueGeneratedNever();

        builder.Property(l => l.LocationType)
            .HasColumnName("location_type")
            .HasConversion<string>();

        builder.Ignore(l => l.IsHoldLocation);
        builder.Ignore(l => l.IsExceptionLocation);
    }
}
