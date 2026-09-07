namespace Throughline.Modules.Ordering.Presentation;

internal sealed record CreateOrderResponse(int OwnerId, Guid OrderId, string OwnerReferenceNumber);