namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IOrderAllocationRepository
{
    void Add(OrderAllocation order);
    Task<OrderAllocation?> GetByOrderIdAsync(int ownerId, Guid orderId, CancellationToken token);
    Task<IReadOnlyCollection<OrderAllocation>> GetAllByOrderIdAsync(IEnumerable<Guid> orderIds);

    /// <summary>
    ///     The owner's orders on <paramref name="worklist" />: at least one line not fully allocated, not currently
    ///     being allocated, and never released (<see cref="AllocationWorklist.Unreleased" />, a null
    ///     <see cref="OrderAllocation.LastUpdated" />) or released (<see cref="AllocationWorklist.Backordered" />,
    ///     a set one). Sorted by <see cref="OrderAllocation.OrderCreatedOn" />, then order id, and limited to the
    ///     first <paramref name="batchSize" />. Never returns another owner's orders.
    /// </summary>
    Task<IReadOnlyCollection<OrderAllocation>> GetWorklistAsync(
        int ownerId, AllocationWorklist worklist, int batchSize, CancellationToken token);
}