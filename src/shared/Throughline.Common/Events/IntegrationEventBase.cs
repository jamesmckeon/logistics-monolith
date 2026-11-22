namespace Throughline.Common.Events;

/// <summary>
///     The time every integration event carries. The publisher supplies it as a constructor parameter, so a
///     handler sees the published value even when the message is rebuilt from storage (a retry, a dead-letter
///     replay, or inbox recovery).
/// </summary>
/// <param name="OccurredOnUtc">When the event happened, at UTC +0</param>
public abstract record IntegrationEventBase(DateTimeOffset OccurredOnUtc);
