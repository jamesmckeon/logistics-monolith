using Throughline.Common.Models;
using Throughline.Common.Results;

namespace Throughline.Modules.Inventory.Domain.Allocation;

/// <summary>
///     OrderAllocation has its Id property typed as <c>Guid</c> rather than <c>EntityId</c> because OrderId is passed
///     by the Ordering module; Inventory can't ensure it matches the GUID spec it uses
/// </summary>
internal sealed class OrderAllocation : Entity<Guid>
{
    private readonly List<OrderLineAllocation> _orderLines;

    private OrderAllocation(
        int ownerId,
        Guid orderId,
        IEnumerable<OrderLineAllocation> orderLines,
        bool allocating)
        : base(orderId)
    {
        ArgumentNullException.ThrowIfNull(orderLines);

        _orderLines = orderLines.ToList();
        OwnerId = ownerId;
        Allocating = allocating;
    }

    // EF materialization constructor
    private OrderAllocation(
        Guid id, int ownerId, bool allocating)
        : base(id)
    {
        _orderLines = [];
        OwnerId = ownerId;
        Allocating = allocating;
    }

    public int OwnerId { get; }

    public IReadOnlyCollection<OrderLineAllocation> OrderLines => _orderLines.AsReadOnly();

    public AllocationStatus AllocationStatus
    {
        get
        {
            if (OrderLines.All(a => a.AllocationStatus == AllocationStatus.Confirmed))
                return AllocationStatus.Confirmed;

            if (OrderLines.Any(a => a.AllocationStatus == AllocationStatus.PartiallyAllocated ||
                                    a.AllocationStatus == AllocationStatus.Confirmed))
                return AllocationStatus.PartiallyAllocated;

            return AllocationStatus.Allocated;
        }
    }

    public AppDateTime? LastUpdated { get; private set; }
    public bool Allocating { get; private set; }
    public bool IsAllocatable => !Allocating && AllocationStatus != AllocationStatus.Allocated;

    public static Result<OrderAllocation> Create(
        int ownerId, Guid orderId, IEnumerable<OrderLineAllocation> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var linesArray = lines.ToArray();

        if (!linesArray.Any())
            return Result<OrderAllocation>.Validation("lines must contain one or more items");

        return new OrderAllocation(ownerId, orderId, linesArray, false);
    }


    public void SetAllocating(AppDateTime started)
    {
        if (AllocationStatus == AllocationStatus.Allocated)
            throw new InvalidOperationException("An order can only be allocated once");

        if (!Allocating)
        {
            Allocating = true;
            LastUpdated = started;
        }
    }

    public void StopAllocating(AppDateTime stopped)
    {
        if (Allocating)
        {
            Allocating = false;
            LastUpdated = stopped;
        }
    }
}