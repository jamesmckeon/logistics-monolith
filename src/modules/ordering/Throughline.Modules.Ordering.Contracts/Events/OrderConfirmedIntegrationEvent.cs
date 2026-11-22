using Throughline.Common.Events;
using Throughline.Modules.Ordering.Contracts.Models;

namespace Throughline.Modules.Ordering.Contracts.Events;

/// <param name="Id">Identifies the event; assigned once, by the publisher</param>
/// <param name="OccurredOnUtc">When the event happened, at UTC +0</param>
/// <param name="CreatedOn">When Throughline accepted the order, at UTC +0</param>
public sealed record OrderConfirmedIntegrationEvent(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    int OwnerId,
    Guid OrderId,
    DateTimeOffset CreatedOn,
    IReadOnlyCollection<OrderLineEventModel> Lines) : IntegrationEventBase(Id, OccurredOnUtc);