using Throughline.Common.Events;
using Throughline.Modules.Ordering.Contracts.Models;

namespace Throughline.Modules.Ordering.Contracts.Events;

/// <param name="CreatedOn">When Throughline accepted the order, at UTC +0</param>
/// <param name="CreatedBy">The Entra object ID (<c>oid</c>) of the caller who submitted the order</param>
public sealed record OrderConfirmedIntegrationEvent(
    int OwnerId,
    Guid OrderId,
    DateTimeOffset CreatedOn,
    IReadOnlyCollection<OrderLineEventModel> Lines,
    Guid? CreatedBy) : IntegrationEventBase(CreatedBy);
