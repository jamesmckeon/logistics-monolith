namespace Throughline.Modules.Ordering.Infrastructure.Orders;

/// <summary>
///     EF persistence model for an order. Owned entirely by Infrastructure and never leaves it;
///     the domain <see cref="Domain.Orders.Order" /> aggregate stays persistence-ignorant.
///     Translation lives in <see cref="OrderMapper" />.
/// </summary>
internal sealed class OrderRecord
{
    public Guid OrderId { get; set; }
    public int MerchantId { get; set; }
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