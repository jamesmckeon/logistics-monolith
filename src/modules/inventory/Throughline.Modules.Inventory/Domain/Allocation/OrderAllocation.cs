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

    public IReadOnlyCollection<OrderLineAllocation> UnallocatedLines =>
        _orderLines.Where(ol => ol.AllocationStatuses != AllocationStatuses.Allocated)
            .ToList().AsReadOnly();

    public AllocationStatuses AllocationStatus
    {
        get
        {
            if (OrderLines.All(a => a.AllocationStatuses == AllocationStatuses.Confirmed))
                return AllocationStatuses.Confirmed;

            if (OrderLines.Any(a => a.AllocationStatuses == AllocationStatuses.PartiallyAllocated ||
                                    a.AllocationStatuses == AllocationStatuses.Confirmed))
                return AllocationStatuses.PartiallyAllocated;

            return AllocationStatuses.Allocated;
        }
    }

    public AppDateTime? LastUpdated { get; private set; }
    public bool Allocating { get; private set; }

    public static Result<OrderAllocation> Create(
        int ownerId, Guid orderId, IEnumerable<OrderLineAllocation> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var linesArray = lines.ToArray();

        if (!linesArray.Any())
            return Result<OrderAllocation>.Validation("lines must contain one or more items");

        var duplicates = linesArray.GroupBy(l => l.SkuId)
            .Select(l => new { SkuId = l.Key, Count = l.Count() })
            .Where(grp => grp.Count > 1);

        if (duplicates.Any())
            return Result<OrderAllocation>.Validation("lines must contain unique Sku IDs");

        return new OrderAllocation(ownerId, orderId, linesArray, false);
    }


    public void StartAllocating(AppDateTime started)
    {
        if (AllocationStatus == AllocationStatuses.Allocated)
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