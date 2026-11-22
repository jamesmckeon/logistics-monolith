using System.Text.Json;

namespace Throughline.Modules.Platform.Application.Models;

/// <param name="DeadLetterId">Wolverine's ID for the dead letter</param>
/// <param name="SentAt">When the message was sent; Wolverine doesn't store when it was dead-lettered</param>
/// <param name="Module">The module whose handler dead-lettered the message</param>
/// <param name="ReplayQueued">
///     True when a replay has marked the dead letter and Wolverine hasn't moved it back to its queue yet
/// </param>
/// <param name="Message">The message, as the JSON Wolverine stored inside the envelope</param>
internal sealed record DeadLetterModel(
    Guid DeadLetterId,
    DateTimeOffset? SentAt,
    string Module,
    string MessageType,
    string? ExceptionType,
    string? ExceptionMessage,
    bool ReplayQueued,
    JsonElement? Message);
