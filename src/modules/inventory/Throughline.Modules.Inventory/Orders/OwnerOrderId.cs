using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Orders;

public sealed class OwnerOrderId : ValueObject
{
    public OwnerOrderId(int ownerId, Guid orderId)
    {
        OwnerId = ownerId;
        OrderId = orderId;
    }

    public int OwnerId { get; }
    public Guid OrderId { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return OwnerId;
        yield return OrderId;
    }
}