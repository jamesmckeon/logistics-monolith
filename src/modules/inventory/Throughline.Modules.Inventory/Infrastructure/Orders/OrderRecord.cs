namespace Throughline.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderRecord
{
    // (OwnerId, OrderId) is the composite identity — the flat form of the domain's
    // OwnerOrderId value object. Kept as scalars so EF Core can key on them directly.
    public int OwnerId { get; set; }
    public Guid OrderId { get; set; }

    public List<OrderLineRecord> OrderLines { get; set; } = [];
}
