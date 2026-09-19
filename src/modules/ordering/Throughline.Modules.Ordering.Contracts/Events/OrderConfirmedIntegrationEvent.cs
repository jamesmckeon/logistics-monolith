using Throughline.Common.Events;
using Throughline.Modules.Ordering.Contracts.Models;

namespace Throughline.Modules.Ordering.Contracts.Events;

public sealed record OrderConfirmedIntegrationEvent(
    int OwnerId,
    Guid OrderId,
    IReadOnlyCollection<OrderLineEventModel> Lines) : IntegrationEventBase;