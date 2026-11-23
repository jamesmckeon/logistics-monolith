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
        NonFutureDateTime orderCreatedOn,
        IEnumerable<OrderLineAllocation> orderLines,
        bool allocating,
        Guid? createdBy)
        : base(orderId)
    {
        ArgumentNullException.ThrowIfNull(orderCreatedOn);
        ArgumentNullException.ThrowIfNull(orderLines);

        _orderLines = orderLines.ToList();
        OwnerId = ownerId;
        OrderCreatedOn = orderCreatedOn;
        Allocating = allocating;
        CreatedBy = createdBy;
    }

    // EF materialization constructor
    private OrderAllocation(
        Guid id, int ownerId, NonFutureDateTime orderCreatedOn, bool allocating, Guid? createdBy)
        : base(id)
    {
        _orderLines = [];
        OwnerId = ownerId;
        OrderCreatedOn = orderCreatedOn;
        Allocating = allocating;
        CreatedBy = createdBy;
    }

    public int OwnerId { get; }

    /// <summary>
    ///     When Throughline accepted the order (Ordering's created date); the allocation worklist is
    ///     sorted by it
    /// </summary>
    public NonFutureDateTime OrderCreatedOn { get; }

    public IReadOnlyCollection<OrderLineAllocation> OrderLines => _orderLines.AsReadOnly();

    public IReadOnlyCollection<OrderLineAllocation> UnallocatedLines =>
        _orderLines.Where(ol => ol.AllocationStatus != AllocationStatuses.Allocated)
            .ToList().AsReadOnly();

    public AllocationStatuses AllocationStatus
    {
        get
        {
            if (OrderLines.All(a => a.AllocationStatus == AllocationStatuses.Confirmed))
                return AllocationStatuses.Confirmed;

            if (OrderLines.Any(a => a.AllocationStatus == AllocationStatuses.PartiallyAllocated ||
                                    a.AllocationStatus == AllocationStatuses.Confirmed))
                return AllocationStatuses.PartiallyAllocated;

            return AllocationStatuses.Allocated;
        }
    }

    /// <summary>
    ///     When an allocation attempt last started or stopped on the order. Only allocation attempts set it, and
    ///     it's null until the first one starts, so it also tells whether the order has been released
    ///     (<see cref="AllocationWorklist" />).
    /// </summary>
    public AppDateTime? LastUpdated { get; private set; }

    public bool Allocating { get; private set; }

    /// <summary>
    ///     The Entra object ID (<c>oid</c>) of the caller who submitted the order. Null for orders confirmed before
    ///     it was recorded, and for messages stored before events carried it.
    /// </summary>
    public Guid? CreatedBy { get; }

    /// <summary>
    ///     The Entra object ID (<c>oid</c>) of the caller whose allocation attempt last started or stopped on the
    ///     order; set with <see cref="LastUpdated" />
    /// </summary>
    public Guid? UpdatedBy { get; private set; }

    /// <param name="orderCreatedOn">When Throughline accepted the order</param>
    /// <param name="createdBy">The Entra object ID (<c>oid</c>) of the caller who submitted the order</param>
    public static Result<OrderAllocation> Create(
        int ownerId,
        Guid orderId,
        NonFutureDateTime orderCreatedOn,
        IEnumerable<OrderLineAllocation> lines,
        Guid? createdBy)
    {
        ArgumentNullException.ThrowIfNull(orderCreatedOn);
        ArgumentNullException.ThrowIfNull(lines);

        var linesArray = lines.ToArray();

        if (!linesArray.Any())
            return Result<OrderAllocation>.Validation("lines must contain one or more items");

        var duplicates = linesArray.GroupBy(l => l.SkuId)
            .Select(l => new { SkuId = l.Key, Count = l.Count() })
            .Where(grp => grp.Count > 1);

        if (duplicates.Any())
            return Result<OrderAllocation>.Validation("lines must contain unique Sku IDs");

        return new OrderAllocation(ownerId, orderId, orderCreatedOn, linesArray, false, createdBy);
    }


    /// <param name="startedBy">The Entra object ID (<c>oid</c>) of the caller allocating the order</param>
    public void StartAllocating(AppDateTime started, Guid startedBy)
    {
        if (AllocationStatus == AllocationStatuses.Allocated)
            throw new InvalidOperationException("An order can only be allocated once");

        if (!Allocating)
        {
            Allocating = true;
            LastUpdated = started;
            UpdatedBy = startedBy;
        }
    }

    /// <param name="stoppedBy">The Entra object ID (<c>oid</c>) of the caller allocating the order</param>
    public void StopAllocating(AppDateTime stopped, Guid stoppedBy)
    {
        if (Allocating)
        {
            Allocating = false;
            LastUpdated = stopped;
            UpdatedBy = stoppedBy;
        }
    }
}