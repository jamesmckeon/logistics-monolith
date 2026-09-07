namespace Throughline.Modules.Ordering.Application.CreateOrder;

public sealed record CreateOrderResult(
    bool Created,
    Guid OrderId,
    int OwnerId,
    string OwnerReferenceNumber);