using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Common;

internal sealed class UniqueId : ValueObject
{
    /// <param name="value">A version 7 GUID</param>
    /// <exception cref="ArgumentException"><paramref name="value" /> is not a version 7 GUID</exception>
    public UniqueId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("value cannot be empty", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static UniqueId Create() => new(Guid.CreateVersion7());
}