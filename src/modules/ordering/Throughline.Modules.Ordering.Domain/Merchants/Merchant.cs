namespace Throughline.Modules.Ordering.Domain.Merchants;

public sealed class Merchant : ValueObject
{
    private Merchant(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Id;
    }

    public static Result<Merchant> Create(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("name is required");

        return new Merchant(id, name);
    }
}