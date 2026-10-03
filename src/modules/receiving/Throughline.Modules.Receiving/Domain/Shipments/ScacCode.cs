using Throughline.Common.Models;
using Throughline.Common.Results;

namespace Throughline.Modules.Receiving.Domain.Shipments;

internal sealed class ScacCode : ValueObject
{
    public ScacCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Trim().Length != 4 || value.Any(a => !char.IsAsciiLetter(a)))
        {
            throw new ArgumentException("value must be 4 alpha characters", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static Result Validate(string value)
    {
        var errors = new List<FieldError>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new("value must be not be blank"));
        }

        if (value.Trim().Length != 4 || value.Any(a => !char.IsAsciiLetter(a)))
        {
            errors.Add(new("value must be 4 alpha characters"));
        }

        return errors.Any() ? Result.Validation(errors) : Result.Success();
    }
}