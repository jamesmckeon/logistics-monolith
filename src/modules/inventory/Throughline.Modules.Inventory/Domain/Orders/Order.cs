using Throughline.Common.Models;
using Throughline.Modules.Inventory.Infrastructure.Orders;

namespace Throughline.Modules.Inventory.Domain.Orders;

public sealed class Order : ValueObject
{
    internal Order(
        OwnerOrderId ownerOrderId,
        IEnumerable<OrderLine> orderLines,
        OrderAllocationStatus allocationStatus)
    {
        ArgumentNullException.ThrowIfNull(ownerOrderId);
        ArgumentNullException.ThrowIfNull(orderLines);

        OwnerOrderId = ownerOrderId;
        OrderLines = orderLines.ToList().AsReadOnly();
        AllocationStatus = allocationStatus;
    }

    public OwnerOrderId OwnerOrderId { get; }
    public IReadOnlyCollection<OrderLine> OrderLines { get; }
    public OrderAllocationStatus AllocationStatus { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return OwnerOrderId;
    }
}