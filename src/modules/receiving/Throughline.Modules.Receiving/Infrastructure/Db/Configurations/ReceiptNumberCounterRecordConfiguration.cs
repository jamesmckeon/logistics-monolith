using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Infrastructure.Db.Models;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class ReceiptNumberCounterRecordConfiguration : IEntityTypeConfiguration<ReceiptNumberCounterRecord>
{
    public void Configure(EntityTypeBuilder<ReceiptNumberCounterRecord> builder)
    {
        builder.ToTable("receipt_number_counters", table =>
            table.HasCheckConstraint("ck_receipt_number_counters_last_number", "last_number > 0"));

        // One row per owner: the key is what makes a second counter for the same owner impossible
        builder.HasKey(c => c.OwnerId).HasName("pk_receipt_number_counters");
        builder.Property(c => c.OwnerId).HasColumnName("owner_id").ValueGeneratedNever();
        builder.Property(c => c.LastNumber).HasColumnName("last_number");
    }
}
