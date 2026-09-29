namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IOrderAllocationRepository
{
    void Add(OrderAllocation order);
    Task<OrderAllocation?> GetByOrderIdAsync(int ownerId, Guid orderId, CancellationToken token);
    Task<IReadOnlyCollection<OrderAllocation>> GetAllByOrderIdAsync(IEnumerable<Guid> orderIds);
}