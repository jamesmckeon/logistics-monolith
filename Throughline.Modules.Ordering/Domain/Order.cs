namespace Throughline.Modules.Ordering.Domain;

public sealed class Order
{
    internal Order(
        OrderId id,
        int merchantId,
        string purchaseOrderNumber,
        string referenceNumber,
        StreetAddress destination,
        IEnumerable<OrderLine> orderLines)
    {
        Id = id;
        MerchantId = merchantId;
        PurchaseOrderNumber = purchaseOrderNumber.Trim();
        ReferenceNumber = referenceNumber.Trim();
        Destination = destination;
        OrderLines = orderLines.ToList().AsReadOnly();
    }

    public OrderId Id { get; }
    public int MerchantId { get; }
    public string PurchaseOrderNumber { get; }
    public string ReferenceNumber { get; }
    public IReadOnlyCollection<OrderLine> OrderLines { get; }
    public StreetAddress Destination { get; }
}