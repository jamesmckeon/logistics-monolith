using Throughline.Common.Models;
using Throughline.Common.Results;

namespace Throughline.Modules.Receiving.Domain.Shipments;

internal sealed class ScacCode : ValueObject
{
    private ScacCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static Result<ScacCode> Create(string value)
    {
        if (value.Trim().Length == 0)
        {
            return Result<ScacCode>.Validation("value cannot be blank");
        }

        if (value.Trim().Length != 4 || value.Any(a => !char.IsAsciiLetter(a)))
        {
            return Result<ScacCode>.Validation("value must be 4 alpha characters");
        }

        return new ScacCode(value.Trim().ToUpperInvariant());
    }
}