using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Orders;

public sealed class Order : ValueObject
{
    public Order(OwnerOrderId ownerOrderId, IEnumerable<OrderLine> orderLines)
    {
        ArgumentNullException.ThrowIfNull(ownerOrderId);
        ArgumentNullException.ThrowIfNull(orderLines);

        var linesArray = orderLines.ToArray().AsReadOnly();

        if (!linesArray.Any())
            throw new ArgumentException("orderLines must contain at least one item", nameof(orderLines));

        OwnerOrderId = ownerOrderId;
        OrderLines = linesArray;
    }

    public OwnerOrderId OwnerOrderId { get; }

    public IReadOnlyCollection<OrderLine> OrderLines { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        throw new NotImplementedException();
    }
}