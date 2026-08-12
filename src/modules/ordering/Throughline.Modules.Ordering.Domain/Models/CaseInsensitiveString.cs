namespace Throughline.Modules.Ordering.Domain.Models;

public class CaseInsensitiveString : ValueObject
{
    public CaseInsensitiveString(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}