using Throughline.Common.Models;
using Throughline.Common.Results;

namespace Throughline.Modules.Inventory.Domain.Allocation;

public sealed class OrderAllocation : Entity<Guid>
{
    private readonly List<OrderLineAllocation> _orderLines;

    internal OrderAllocation(
        int ownerId,
        Guid orderId,
        IEnumerable<OrderLineAllocation> orderLines,
        OrderAllocationStatus allocationStatus)
        : base(orderId)
    {
        ArgumentNullException.ThrowIfNull(orderLines);

        _orderLines = orderLines.ToList();
        AllocationStatus = allocationStatus;
        OwnerId = ownerId;
    }

    // EF materialization constructor
    private OrderAllocation(Guid id, int ownerId, OrderAllocationStatus allocationStatus)
        : base(id)
    {
        _orderLines = [];
        OwnerId = ownerId;
        AllocationStatus = allocationStatus;
    }

    public int OwnerId { get; }

    public IReadOnlyCollection<OrderLineAllocation> OrderLines => _orderLines.AsReadOnly();

    public OrderAllocationStatus AllocationStatus { get; private set; }
    public AppDateTime? AllocationStatusUpdated { get; private set; }

    public static Result<OrderAllocation> Create(
        int ownerId, Guid orderId, IEnumerable<OrderLineAllocation> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var linesArray = lines.ToArray();

        if (!linesArray.Any())
            return Result<OrderAllocation>.Validation("lines must contain one or more items");

        return new OrderAllocation(ownerId, orderId, linesArray, OrderAllocationStatus.Confirmed);
    }


    public Result SetAllocating(AppDateTime started)
    {
        if (AllocationStatus == OrderAllocationStatus.Allocating)
            return Result.Conflict("Order is already allocating");

        if (AllocationStatus == OrderAllocationStatus.Allocated)
            return Result.Conflict("An order can only be allocated once");

        AllocationStatus = OrderAllocationStatus.Allocating;
        AllocationStatusUpdated = started;

        return Result.Success();
    }
}