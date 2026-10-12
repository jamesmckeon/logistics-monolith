using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Configurations;

internal sealed class HoldReasonConfiguration : IEntityTypeConfiguration<HoldReason>
{
    public void Configure(EntityTypeBuilder<HoldReason> builder)
    {
        builder.ToTable("hold_reasons");

        // Hold reasons are one warehouse-wide list, shared by every owner.
        builder.HasKey(h => h.ReasonCode).HasName("pk_hold_reasons");

        builder.Property(h => h.ReasonCode).HasColumnName("reason_code");
        builder.Property(h => h.IsActive).HasColumnName("is_active");
    }
}
