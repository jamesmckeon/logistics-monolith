using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Common;

internal sealed class UpperCaseString : ValueObject
{
    public UpperCaseString(string value)
    {
        if (value.Trim().Length == 0)
        {
            throw new ArgumentException("value cannot be blank", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }


    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}