namespace Throughline.Common.Events;

/// <summary>
///     The identity and time every integration event carries. The publisher supplies both, and they're
///     constructor parameters, so a handler sees the published values even when the message is rebuilt from
///     storage (a retry, a dead-letter replay, or inbox recovery).
/// </summary>
/// <param name="Id">Identifies the event; assigned once, by the publisher</param>
/// <param name="OccurredOnUtc">When the event happened, at UTC +0</param>
public abstract record IntegrationEventBase(Guid Id, DateTimeOffset OccurredOnUtc);
