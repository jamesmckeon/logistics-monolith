namespace Throughline.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderLineRecord
{
    public int Id { get; set; } // surrogate PK for the row

    // Composite FK back to OrderRecord.
    public int OwnerId { get; set; }
    public Guid OrderId { get; set; }

    public string SkuCode { get; set; } = null!;
    public int QuantityRequested { get; set; }
}
