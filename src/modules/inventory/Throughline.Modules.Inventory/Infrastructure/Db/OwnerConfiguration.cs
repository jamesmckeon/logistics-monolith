using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("owners");

        // Owner ids are assigned outside Inventory (the client-owner master), never generated here.
        builder.HasKey(k => k.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.AllocationPolicy)
            .HasColumnName("allocation_policy")
            .HasConversion<string>();
    }
}