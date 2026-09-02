namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrderLineRecord
{
    public int Id { get; set; } // surrogate PK for the row
    public Guid OrderId { get; set; } // FK back to OrderRecord
    public string SkuCode { get; set; } = null!;
    public int Quantity { get; set; }
}