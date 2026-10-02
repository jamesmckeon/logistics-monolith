using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Shipments;

internal sealed class CarrierScac : ValueObject
{
    public CarrierScac(ScacCode scacCode, int carrierId, string carrierName)
    {
        ScacCode = scacCode;
        CarrierId = carrierId;
        CarrierName = carrierName;
    }

    public ScacCode ScacCode { get; }
    public int CarrierId { get; }
    public string CarrierName { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return ScacCode;
    }
}