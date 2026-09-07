using Throughline.Common.Models;
using Throughline.Common.Results;

namespace Throughline.Modules.Ordering.Domain.Orders;

/// <summary>
///     Immutable value object holding an order's substantive content: its purchase order
///     number, destination address, and order lines. Two instances are equal when all three
///     match, regardless of the sort order of a instance's <c>OrderLines</c> property.
///     Use <see cref="Create" /> to build a validated instance.
/// </summary>
internal sealed class OrderContent : ValueObject
{
    public OrderContent(
        string purchaseOrderNumber, StreetAddress destination, IEnumerable<OrderLine> orderLines)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrderNumber);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(orderLines);

        PurchaseOrderNumber = purchaseOrderNumber;
        Destination = destination;
        OrderLines = orderLines.ToList().AsReadOnly();
    }

    public string PurchaseOrderNumber { get; }
    public IReadOnlyCollection<OrderLine> OrderLines { get; }
    public StreetAddress Destination { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return PurchaseOrderNumber;
        yield return Destination;
        foreach (var line in OrderLines
                     .OrderBy(o => o.SkuCode.Value)
                     .ThenBy(o => o.Quantity))
            yield return line;
    }

    public static Result<OrderContent> Create(
        string purchaseOrderNumber,
        StreetAddress destination,
        IEnumerable<OrderLine> orderLines)
    {
        var errors = new List<Error>();
        if (purchaseOrderNumber.Trim() == "")
            errors.Add(new("purchaseOrderNumber is required"));

        var linesArray = orderLines.ToArray();
        if (!linesArray.Any())
            errors.Add(new Error("An order must have at least one line"));

        var duplicates = linesArray
            .GroupBy(grp => grp.SkuCode)
            .Select(s => new { SkuCode = s.Key, Count = s.Count() })
            .Where(w => w.Count > 1);

        if (duplicates.Any())
            errors.Add(new("An order cannot have more than one line with the same sku code"));

        return errors.Any()
            ? Result<OrderContent>.Validation(errors)
            : new OrderContent(purchaseOrderNumber.Trim(), destination, linesArray);
    }
}