using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Infrastructure.Db;

internal sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("owners");

        builder.HasKey(k => k.Id);
        builder.Property(p => p.AllocationPolicy)
            .HasColumnName("allocation_policy")
            .HasConversion<string>();

    }
}