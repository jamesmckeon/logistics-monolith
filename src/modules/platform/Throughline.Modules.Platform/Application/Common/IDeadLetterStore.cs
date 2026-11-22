using Throughline.Modules.Platform.Application.Models;

namespace Throughline.Modules.Platform.Application.Common;

/// <summary>
///     Reads Wolverine's dead-letter table directly
/// </summary>
internal interface IDeadLetterStore
{
    /// <summary>
    ///     Every dead letter in the table, read in one query, in no particular order
    /// </summary>
    Task<IReadOnlyCollection<DeadLetterRecord>> GetDeadLettersAsync(CancellationToken token);

    /// <summary>
    ///     The dead letter with <paramref name="deadLetterId" />, or null when the table has none
    /// </summary>
    Task<DeadLetterRecord?> GetDeadLetterAsync(Guid deadLetterId, CancellationToken token);
}
