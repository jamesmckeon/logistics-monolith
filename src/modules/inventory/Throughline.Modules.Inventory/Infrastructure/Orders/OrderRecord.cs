namespace Throughline.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderRecord
{
    public int OwnerId { get; set; }
    public Guid OrderId { get; set; }
    public OrderAllocationStatus AllocationStatus { get; set; }
    public List<OrderLineRecord> OrderLines { get; set; } = [];
}