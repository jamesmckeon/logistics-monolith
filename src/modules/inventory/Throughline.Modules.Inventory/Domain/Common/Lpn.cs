using Throughline.Common.Models;
using Throughline.Common.Results;

namespace Throughline.Modules.Inventory.Domain.Common;

internal class Lpn : ValueObject
{
    private Lpn(string value)
    {
        Value = value;
    }

    public string Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        throw new NotImplementedException();
    }

    public static Result<Lpn> Create(string lpn)
    {
        throw new NotImplementedException("validate SSCC format");
    }

    public override string ToString()
    {
        return Value;
    }
}