namespace Throughline.SharedKernel.Merchants;

public sealed class Merchant : ValueObject
{
    public Merchant(int id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Id;
    }
}