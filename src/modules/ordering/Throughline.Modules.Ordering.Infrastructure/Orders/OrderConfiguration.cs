using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Orders;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.Property(f => f.Id)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .HasColumnName("OrderId")
            .HasColumnType("uuid");

        builder.HasKey(f => f.Id);

        builder.Property(p => p.PurchaseOrderNumber)
            .HasMaxLength(50);

        builder.Property(p => p.ReferenceNumber)
            .HasMaxLength(50);

        builder.ComplexProperty(p => p.Destination, dest =>
        {
            dest.Property(p => p.StreeAddressOne)
                .HasColumnName("StreetAddressOne")
                .HasMaxLength(150);

            dest.Property(p => p.StreetAddressTwo)
                .HasColumnName("StreetAddressTwo")
                .HasMaxLength(150);

            dest.Property(p => p.City)
                .HasColumnName("City")
                .HasMaxLength(50);

            dest.Property(p => p.State)
                .HasColumnName("State")
                .HasMaxLength(2)
                .IsFixedLength();

            dest.Property(p => p.ZipCode)
                .HasConversion(c => c.Value, val => new PostalCode(val))
                .HasColumnName("Zipcode")
                .HasMaxLength(10);
        });

        builder.Property(f => f.DestinationSurcharge)
            .HasConversion(c => c.Value, value => new Money(value))
            .HasColumnName("DestinationSurcharge")
            .HasPrecision(18, 4);

        builder.Property(f => f.TotalCharges)
            .HasConversion(c => c.Value, value => new Money(value))
            .HasColumnName("TotalCharges")
            .HasPrecision(18, 4);

        builder.Property(f => f.TotalWeight)
            .HasColumnName("TotalWeight")
            .HasPrecision(18, 4);

        builder.OwnsMany(o => o.OrderLines, line =>
        {
            line.ToTable("OrderLines");
            line.WithOwner().HasForeignKey("OrderId");
            line.Property<int>("Id");
            line.HasKey("Id");

            line.Property(l => l.SkuCode)
                .HasConversion(c => c.Value, v => new SkuCode(v))
                .HasColumnName("SkuCode")
                .HasMaxLength(50);

            line.Property(l => l.UnitPickFee)
                .HasConversion(r => r.Value, v => new Rate(v))
                .HasColumnName("UnitPickFee")
                .HasPrecision(18, 4);

            line.Property(l => l.TotalPickFees)
                .HasConversion(m => m.Value, v => new Money(v))
                .HasPrecision(18, 4);

            line.Property(l => l.TotalWeight)
                .HasPrecision(18, 4);

            line.Property(l => l.TotalHandling)
                .HasConversion(m => m.Value, v => new Money(v))
                .HasPrecision(18, 4);
        });
    }
}