namespace Throughline.Simulator.Ledger;

/// <summary>
///     A line of an order Throughline accepted; the customer's sales history
/// </summary>
internal sealed record SoldLine(string Sku, int Quantity);

internal enum PurchaseOrderKind
{
    Opening,
    Weekly
}

/// <summary>
///     A customer's order to its supplier, delivered to the warehouse as one or more trailers
/// </summary>
internal sealed record PurchaseOrder(
    Guid PurchaseOrderId,
    int OwnerId,
    string PurchaseOrderNumber,
    PurchaseOrderKind Kind,
    DateTimeOffset OrderedAt,
    IReadOnlyList<InboundShipment> Shipments);

/// <summary>
///     One trailer on its way to the dock. Its id is sent as the receipt id, so resending a receipt
///     for the same trailer is recognized by Throughline as the same delivery.
/// </summary>
internal sealed record InboundShipment(
    Guid ShipmentId,
    int OwnerId,
    string DeliveryReference,
    string BillOfLading,
    string CarrierScac,
    string TrailerNumber,
    string ShipperName,
    DateTimeOffset DueAt,
    IReadOnlyList<InboundPallet> Pallets);

/// <summary>
///     A single-SKU pallet and the location it's put away to
/// </summary>
internal sealed record InboundPallet(string Lpn, string Sku, int Quantity, string LocationId);
