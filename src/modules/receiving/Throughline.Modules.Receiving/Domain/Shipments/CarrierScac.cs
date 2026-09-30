using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Shipments;

internal sealed class CarrierScac : ValueObject
{
    public CarrierScac(ScacCode scacCode, int carrierId)
    {
        ScacCode = scacCode;
        CarrierId = carrierId;
    }

    public ScacCode ScacCode { get; }
    public int CarrierId { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return ScacCode;
    }
}