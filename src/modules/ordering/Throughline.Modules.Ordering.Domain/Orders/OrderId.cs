using Throughline.Modules.Ordering.Domain.Models;

namespace Throughline.Modules.Ordering.Domain.Orders;

public sealed class OrderId : ValueObject
{
    public OrderId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    internal OrderId()
    {
        Value = Guid.CreateVersion7();
    }

    public Guid Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}