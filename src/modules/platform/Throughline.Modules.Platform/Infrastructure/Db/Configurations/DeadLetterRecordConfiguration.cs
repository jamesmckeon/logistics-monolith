using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Throughline.Modules.Platform.Application.Models;
using Throughline.Modules.Platform.Infrastructure.Common;

namespace Throughline.Modules.Platform.Infrastructure.Db.Configurations;

/// <summary>
///     Maps Wolverine's dead-letter table. Liquibase owns the table (db/changelog/wolverine), so a Wolverine upgrade
///     that changes it changes this mapping in the same pull request.
/// </summary>
internal sealed class DeadLetterRecordConfiguration : IEntityTypeConfiguration<DeadLetterRecord>
{
    public void Configure(EntityTypeBuilder<DeadLetterRecord> builder)
    {
        builder.ToTable("wolverine_dead_letters", InfrastructureSettings.WolverineSchemaName);

        builder.HasKey(d => d.Id).HasName("pkey_wolverine_dead_letters_id");
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(d => d.Body).HasColumnName("body");
        builder.Property(d => d.MessageType).HasColumnName("message_type");
        builder.Property(d => d.ExceptionType).HasColumnName("exception_type");
        builder.Property(d => d.ExceptionMessage).HasColumnName("exception_message");
        builder.Property(d => d.SentAt).HasColumnName("sent_at");
        builder.Property(d => d.Replayable).HasColumnName("replayable");
    }
}
