using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Shipments;

internal sealed class CarrierScac : ValueObject
{
    public CarrierScac(ScacCode scacCode, string name)
    {
        ScacCode = scacCode;
        Name = name;
    }

    public ScacCode ScacCode { get; }
    public string Name { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return ScacCode;
        yield return Name;
    }
}