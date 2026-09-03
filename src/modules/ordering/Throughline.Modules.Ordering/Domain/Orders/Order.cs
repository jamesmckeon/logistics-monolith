using Throughline.Common.Results;

namespace Throughline.Modules.Ordering.Domain.Orders;

internal sealed class Order
{
    internal Order(
        OrderId id,
        int ownerId,
        string purchaseOrderNumber,
        string referenceNumber,
        StreetAddress destination,
        IEnumerable<OrderLine> orderLines)
    {
        Id = id;
        OwnerId = ownerId;
        PurchaseOrderNumber = purchaseOrderNumber.Trim();
        ReferenceNumber = referenceNumber.Trim();
        Destination = destination;
        OrderLines = orderLines.ToList().AsReadOnly();
    }

    public OrderId Id { get; }
    public int OwnerId { get; }
    public string PurchaseOrderNumber { get; }
    public string ReferenceNumber { get; }
    public IReadOnlyCollection<OrderLine> OrderLines { get; }
    public StreetAddress Destination { get; }

    internal static Result<Order> Create(
        OrderId orderId,
        int ownerId,
        string purchaseOrderNumber,
        string referenceNumber,
        StreetAddress destination,
        IEnumerable<OrderLine> orderLines)
    {
        ArgumentNullException.ThrowIfNull(orderId);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(orderLines);
        ArgumentNullException.ThrowIfNull(purchaseOrderNumber);
        ArgumentNullException.ThrowIfNull(referenceNumber);

        var linesArray = orderLines.ToArray();

        if (linesArray.Length == 0)
            return Result<Order>.Validation("An order must have at least one line");

        var duplicateSkus = linesArray.GroupBy(g => g.SkuCode)
            .Select(s => new { Sku = s.Key, Count = s.Count() })
            .Where(s => s.Count > 1)
            .Select(s => s.Sku)
            .ToList();

        if (duplicateSkus.Any())
            return Result<Order>.Validation(
                "An order cannot have more than one line with the same sku code");

        return new Order(
            orderId,
            ownerId,
            purchaseOrderNumber,
            referenceNumber,
            destination,
            linesArray);
    }
}