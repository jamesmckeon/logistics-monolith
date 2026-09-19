using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Orders;

internal sealed class OwnerOrder : ValueObject
{
    public int OwnerId { get; }
    public Guid OrderId { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        throw new NotImplementedException();
    }
}