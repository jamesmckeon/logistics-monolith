using System.Text.Json;
using Throughline.Common.Results;
using Throughline.Modules.Platform.Application.Common;
using Throughline.Modules.Platform.Application.Models;
using Wolverine.Runtime.Serialization;

namespace Throughline.Modules.Platform.Application.Queries;

internal sealed class GetDeadLettersQuery
{
    /// <summary>
    ///     The module every dead letter is reported against. Today each integration event has one handler, and both
    ///     handlers (<c>OrderConfirmedHandler</c>, <c>AllocatablePalletsHandler</c>) are in Inventory. Wolverine
    ///     doesn't store which handler dead-lettered a message in a column that can be filtered on. This has to be
    ///     replaced, in the same change, when a module other than Inventory starts handling integration events: the
    ///     module then has to come from somewhere reliable, such as a named queue per handler.
    /// </summary>
    public const string DefaultModule = "Inventory";

    private readonly IDeadLetterStore _store;

    public GetDeadLettersQuery(IDeadLetterStore store)
    {
        _store = store;
    }

    /// <summary>
    ///     Returns every dead letter in Wolverine's table that matches the filters, newest first by when the message
    ///     was sent, with dead letters that have no sent time last. The table is read once, through
    ///     <see cref="IDeadLetterStore.GetDeadLettersAsync" />, and the filters are applied after reading.
    /// </summary>
    /// <param name="ownerId">
    ///     When given, only dead letters whose message has an <c>ownerId</c> property (matched ignoring case) equal
    ///     to this owner ID are returned. A dead letter whose message has no owner ID is never returned for an
    ///     owner. When null, dead letters aren't filtered by owner.
    /// </param>
    /// <param name="module">
    ///     When given, only dead letters reported against this module are returned, matched ignoring case; see
    ///     <see cref="DefaultModule" />. When null, dead letters aren't filtered by module.
    /// </param>
    /// <returns>
    ///     The matching dead letters, which may be none. Each carries Wolverine's ID, when the message was sent,
    ///     the module, the message type, the exception type and message, whether a replay is queued (true only when
    ///     Wolverine's replayable flag is true) and the message: the JSON inside the envelope stored in the row's
    ///     body.
    /// </returns>
    public async Task<Result<DeadLetterListModel>> GetDeadLettersAsync(
        int? ownerId, string? module, CancellationToken token)
    {
        var records = await _store.GetDeadLettersAsync(token);

        // Every dead letter is reported against the default module, so any other module matches none
        if (module is not null && !string.Equals(module, DefaultModule, StringComparison.OrdinalIgnoreCase))
        {
            return new DeadLetterListModel([]);
        }

        // A null SentAt sorts below any value, so descending order puts dead letters with no sent time last
        var deadLetters = records
            .Select(ToModel)
            .Where(d => ownerId is null || HasOwner(d.Message, ownerId.Value))
            .OrderByDescending(d => d.SentAt)
            .ToArray();

        return new DeadLetterListModel(deadLetters);
    }

    /// <summary>
    ///     Returns one dead letter from Wolverine's table, with the same details as
    ///     <see cref="GetDeadLettersAsync" />.
    /// </summary>
    /// <returns>
    ///     The dead letter; or a not-found failure when the table has no dead letter with
    ///     <paramref name="deadLetterId" />, including one that was discarded or already moved back by a replay
    /// </returns>
    public async Task<Result<DeadLetterModel>> GetDeadLetterAsync(Guid deadLetterId, CancellationToken token)
    {
        var record = await _store.GetDeadLetterAsync(deadLetterId, token);

        return record is null
            ? Result<DeadLetterModel>.NotFound($"Dead letter {deadLetterId} was not found.")
            : ToModel(record);
    }

    private static DeadLetterModel ToModel(DeadLetterRecord record) =>
        new(
            record.Id,
            record.SentAt,
            DefaultModule,
            record.MessageType,
            record.ExceptionType,
            record.ExceptionMessage,
            record.Replayable == true,
            ReadMessage(record.Body));

    /// <summary>
    ///     Unpacks the envelope Wolverine serialized into the row's body and returns the message JSON inside it
    /// </summary>
    private static JsonElement? ReadMessage(byte[] body)
    {
        var data = EnvelopeSerializer.Deserialize(body).Data;

        return data is null || data.Length == 0 ? null : JsonSerializer.Deserialize<JsonElement>(data);
    }

    private static bool HasOwner(JsonElement? message, int ownerId)
    {
        if (message is not { ValueKind: JsonValueKind.Object } json)
        {
            return false;
        }

        return json.EnumerateObject().Any(p =>
            string.Equals(p.Name, "ownerId", StringComparison.OrdinalIgnoreCase)
            && p.Value.ValueKind == JsonValueKind.Number
            && p.Value.TryGetInt32(out var value)
            && value == ownerId);
    }
}
