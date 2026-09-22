namespace Throughline.Modules.Inventory.Domain.Allocation;

internal interface IOrderAllocationRepository
{
    void Add(OrderAllocation order);
    Task<OrderAllocation?> GetByOrderId(Guid orderId, CancellationToken token);
    Task<IReadOnlyCollection<OrderAllocation>> GetAllByOrderId(IEnumerable<Guid> orderIds);
}