using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Common;

/// <summary>
///     A non blank trimmed string
/// </summary>
internal sealed class TrimmedString : ValueObject
{
    public TrimmedString(string value)
    {
        if (value.Trim().Length == 0)
        {
            throw new ArgumentException("value cannot be blank", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}