using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Infrastructure.Db;

internal sealed class HoldReasonConfiguration : IEntityTypeConfiguration<HoldReason>
{
    public const string OwnerIdProperty = "owner_id";

    public void Configure(EntityTypeBuilder<HoldReason> builder)
    {
        builder.ToTable("hold_reasons");

        // Hold reasons are defined per owner. HoldReason itself doesn't carry the owner, so it is a shadow
        // property: part of the key and the filter, invisible to the domain.
        builder.Property<int>(OwnerIdProperty).HasColumnName("owner_id");
        builder.HasKey(OwnerIdProperty, nameof(HoldReason.ReasonCode)).HasName("pk_hold_reasons");

        builder.Property(h => h.ReasonCode).HasColumnName("reason_code");
        builder.Property(h => h.IsActive).HasColumnName("is_active");
    }
}
