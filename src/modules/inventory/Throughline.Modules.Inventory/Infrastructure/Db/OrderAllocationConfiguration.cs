using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class OrderAllocationConfiguration : IEntityTypeConfiguration<OrderAllocation>
{
    // SaveConfirmedOrder matches on this to distinguish a duplicate order (safe to swallow)
    // from any other unique violation (a real error worth rethrowing).
    public const string PrimaryKeyName = "pk_order_allocations";

    public void Configure(EntityTypeBuilder<OrderAllocation> builder)
    {
        builder.ToTable("order_allocations");

        // OrderId is a globally-unique UUIDv7, so it is the identity; owner is a scope attribute.
        builder.HasKey(o => o.Id).HasName(PrimaryKeyName);
        builder.Property(o => o.Id).HasColumnName("order_id").HasColumnType("uuid");

        builder.Property(o => o.OwnerId).HasColumnName("owner_id");
        builder.HasIndex(o => o.OwnerId);

        builder.Property(o => o.AllocationStatus).HasColumnName("allocation_status")
            .HasConversion<string>();

        builder.Property(o => o.AllocationStatusUpdated)
            .HasColumnName("allocation_status_updated")
            .HasConversion(
                v => v!.Value,
                v => new AppDateTime(v));

        // Optimistic concurrency via PostgreSQL's xmin system column
        builder.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.OwnsMany(o => o.OrderLines, line =>
        {
            line.ToTable("orderline_allocations");
            line.WithOwner().HasForeignKey("order_id");
            line.Property<Guid>("order_id").HasColumnType("uuid");
            line.Property(l => l.SkuId).HasColumnName("sku_id").HasColumnType("uuid");
            line.Property(l => l.QuantityRequested).HasColumnName("quantity_requested");
        });

        // OrderLines is an encapsulated read-only view over the _orderLines backing field.
        builder.Navigation(o => o.OrderLines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}