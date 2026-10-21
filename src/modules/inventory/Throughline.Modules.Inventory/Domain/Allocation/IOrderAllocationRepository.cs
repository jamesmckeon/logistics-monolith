namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IOrderAllocationRepository
{
    void Add(OrderAllocation order);
    Task<OrderAllocation?> GetByOrderIdAsync(int ownerId, Guid orderId, CancellationToken token);
    Task<IReadOnlyCollection<OrderAllocation>> GetAllByOrderIdAsync(IEnumerable<Guid> orderIds);

    /// <summary>
    ///     The owner's orders awaiting allocation: at least one line not fully allocated, and not currently
    ///     being allocated. Sorted by <see cref="OrderAllocation.OrderCreatedOn" />, then order id, and limited
    ///     to the first <paramref name="batchSize" />. Never returns another owner's orders.
    /// </summary>
    Task<IReadOnlyCollection<OrderAllocation>> GetAwaitingAllocationAsync(
        int ownerId, int batchSize, CancellationToken token);
}