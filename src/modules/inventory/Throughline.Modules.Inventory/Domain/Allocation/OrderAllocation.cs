using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Orders;
using Throughline.Modules.Inventory.Infrastructure.Orders;

namespace Throughline.Modules.Inventory.Domain.Allocation;

public sealed class Order : Entity<OwnerOrderId>
{
    internal Order(
        OwnerOrderId ownerOrderId,
        IEnumerable<OrderLineAllocation> orderLines,
        OrderAllocationStatus allocationStatus)
        : base(ownerOrderId)
    {
        ArgumentNullException.ThrowIfNull(orderLines);

        OrderLines = orderLines.ToList().AsReadOnly();
        AllocationStatus = allocationStatus;
    }

    public IReadOnlyCollection<OrderLineAllocation> OrderLines { get; }
    public OrderAllocationStatus AllocationStatus { get; }
}