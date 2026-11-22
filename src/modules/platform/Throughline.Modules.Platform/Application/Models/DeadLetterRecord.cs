namespace Throughline.Modules.Platform.Application.Models;

/// <summary>
///     A row of Wolverine's dead-letter table (<c>wolverine.wolverine_dead_letters</c>), as Wolverine stored it
/// </summary>
internal sealed class DeadLetterRecord
{
    public Guid Id { get; init; }

    /// <summary>
    ///     The whole envelope, in Wolverine's binary envelope format; the message is inside it, as JSON
    /// </summary>
    public required byte[] Body { get; init; }

    public required string MessageType { get; init; }
    public string? ExceptionType { get; init; }
    public string? ExceptionMessage { get; init; }

    /// <summary>
    ///     When the message was sent; Wolverine doesn't store when it was dead-lettered
    /// </summary>
    public DateTimeOffset? SentAt { get; init; }

    /// <summary>
    ///     True when a replay has marked the dead letter and Wolverine hasn't moved it back to its queue yet
    /// </summary>
    public bool? Replayable { get; init; }
}
