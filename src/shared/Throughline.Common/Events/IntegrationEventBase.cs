namespace Throughline.Common.Events;

/// <summary>
///     What every integration event carries. The publisher supplies it as a constructor parameter, so a handler sees
///     the published value even when the message is rebuilt from storage (a retry, a dead-letter replay, or inbox
///     recovery).
/// </summary>
/// <param name="CreatedBy">
///     The Entra object ID (<c>oid</c>) of the caller whose request caused the event; the handler records it as who
///     created or changed its own records. Null only on messages stored before events carried it.
/// </param>
public abstract record IntegrationEventBase(Guid? CreatedBy);
