using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Orders;

public sealed class OrderId : ValueObject
{
    public OrderId(Guid value)
    {
        Value = value;
    }

    public OrderId()
    {
        Value = Guid.CreateVersion7();
    }

    public Guid Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}