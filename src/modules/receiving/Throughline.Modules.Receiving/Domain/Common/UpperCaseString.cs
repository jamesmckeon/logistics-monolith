using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Common;

internal sealed class UpperCaseString : ValueObject
{
    private UpperCaseString(string value)
    {
        Value = value;
    }

    public string Value { get; }


    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static UpperCaseString Create(string value)
    {
        if (value.Trim() == string.Empty)
        {
            throw new ArgumentException("value cannot be blank", nameof(value));
        }

        return new UpperCaseString(value.Trim().ToUpperInvariant());
    }
}