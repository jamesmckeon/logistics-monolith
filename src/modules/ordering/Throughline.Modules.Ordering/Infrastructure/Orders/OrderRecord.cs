namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed class OrderRecord
{
    public Guid OrderId { get; set; }
    public int OwnerId { get; set; }
    public string PurchaseOrderNumber { get; set; } = null!;
    public string ReferenceNumber { get; set; } = null!;

    // Destination value object flattened to columns.
    public string StreetAddressOne { get; set; } = null!;
    public string? StreetAddressTwo { get; set; }
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string Zipcode { get; set; } = null!;

    public List<OrderLineRecord> OrderLines { get; set; } = [];
}