namespace Throughline.Modules.Receiving.Infrastructure.Db.Models;

/// <summary>
///     The last receipt number issued to an owner; one row per owner. Mapped so the migration creates the table,
///     but only ever read and written by the statement in <see cref="ReceivingDbContext.NextReceiptNumberAsync" />.
/// </summary>
internal sealed class ReceiptNumberCounterRecord
{
    public int OwnerId { get; init; }
    public long LastNumber { get; init; }
}
