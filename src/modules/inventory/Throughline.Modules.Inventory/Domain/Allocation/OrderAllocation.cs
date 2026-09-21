using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class OrderAllocation : Entity<Guid>
{
    private readonly List<OrderLineAllocation> _orderLines;

    private OrderAllocation(
        int ownerId,
        Guid orderId,
        IEnumerable<OrderLineAllocation> orderLines,
        AllocationStatus allocationStatus,
        bool allocating)
        : base(orderId)
    {
        ArgumentNullException.ThrowIfNull(orderLines);

        _orderLines = orderLines.ToList();
        AllocationStatus = allocationStatus;
        OwnerId = ownerId;
        Allocating = allocating;
    }

    // EF materialization constructor
    private OrderAllocation(
        Guid id, int ownerId, AllocationStatus allocationStatus, bool allocating)
        : base(id)
    {
        _orderLines = [];
        OwnerId = ownerId;
        AllocationStatus = allocationStatus;
        Allocating = allocating;
    }

    public int OwnerId { get; }

    public IReadOnlyCollection<OrderLineAllocation> OrderLines => _orderLines.AsReadOnly();

    public AllocationStatus AllocationStatus { get; }
    public AppDateTime? AllocationStatusUpdated { get; private set; }
    public bool Allocating { get; private set; }
    public bool IsAllocatable => !Allocating && AllocationStatus != AllocationStatus.Allocated;

    public static Result<OrderAllocation> Create(
        int ownerId, Guid orderId, IEnumerable<OrderLineAllocation> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var linesArray = lines.ToArray();

        if (!linesArray.Any())
            return Result<OrderAllocation>.Validation("lines must contain one or more items");

        return new OrderAllocation(ownerId, orderId, linesArray, AllocationStatus.Confirmed, false);
    }


    public void SetAllocating(AppDateTime started)
    {
        if (Allocating)
            throw new InvalidOperationException("Order is already allocating");

        if (AllocationStatus == AllocationStatus.Allocated)
            throw new InvalidOperationException("An order can only be allocated once");

        Allocating = true;
    }

    public void StopAllocating()
    {
        if (!Allocating)
            throw new InvalidOperationException("Order isn't allocating");

        Allocating = false;
    }

    public void UpdateLine(Sku sku, int quantityAllocated, AppDateTime updatedOn)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityAllocated);

        var line = OrderLines.SingleOrDefault(s => s.Sku == sku);

        if (line == null)
            throw new ArgumentException($"Order doesn't contain a line for sku {sku}");

        line.UpdateAllocatedQuantity(quantityAllocated, updatedOn);
    }
}