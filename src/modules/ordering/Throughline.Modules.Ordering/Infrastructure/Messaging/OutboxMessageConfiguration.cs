using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Throughline.Modules.Ordering.Infrastructure.Messaging;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public const string OwnerReferenceIndexName = "IX_Orders_UniqueOwnerReference";

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(o => o.Error)
            .HasMaxLength(500);

        builder.Property(o => o.Type)
            .HasMaxLength(50);
    }
}