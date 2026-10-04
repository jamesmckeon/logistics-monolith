using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Infrastructure.Db.Converters;

namespace Throughline.Modules.Receiving.Infrastructure.Db;

internal sealed class DeliverySubmissionConfiguration : IEntityTypeConfiguration<DeliverySubmission>
{
    // Two concurrent submissions of the same delivery collide here
    public const string PrimaryKeyName = "pk_delivery_submissions";

    public void Configure(EntityTypeBuilder<DeliverySubmission> builder)
    {
        builder.ToTable("delivery_submissions");

        // The delivery id is client-assigned, so a submission is identified within its owner.
        builder.HasKey(s => new { s.OwnerId, s.DeliveryId }).HasName(PrimaryKeyName);
        builder.Property(s => s.OwnerId).HasColumnName("owner_id");
        builder.Property(s => s.DeliveryId)
            .HasColumnName("delivery_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        // Plain text, not jsonb: resubmissions are compared to it by exact string equality, and jsonb would
        // reorder keys and drop whitespace.
        builder.Property(s => s.Request).HasColumnName("request");

        // A snapshot of the response as first returned; only ever replayed whole, never queried into.
        builder.Property(s => s.Result)
            .HasColumnName("result")
            .HasColumnType("jsonb")
            .HasConversion<ReceiveDeliveryResultValueConverter>();
    }
}
