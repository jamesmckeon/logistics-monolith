namespace Throughline.SharedKernel.Skus;

public sealed class SkuCode : ValueObject
{
    public string Value { get; }

    public SkuCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}