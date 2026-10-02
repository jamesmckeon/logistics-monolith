using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Common;

internal sealed class UniqueId : ValueObject
{
    private UniqueId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static UniqueId Create() => new(Guid.CreateVersion7());
}