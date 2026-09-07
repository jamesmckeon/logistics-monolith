using Throughline.Modules.Ordering.Application.CreateOrder;

namespace Throughline.Modules.Ordering.Presentation;

internal sealed record CreateOrderResponse(int OwnerId, Guid OrderId, string OwnerReferenceNumber)
{
    public static CreateOrderResponse FromResult(CreateOrderResult result)
    {
        return new CreateOrderResponse(result.OwnerId, result.OrderId, result.OwnerReferenceNumber);
    }
}