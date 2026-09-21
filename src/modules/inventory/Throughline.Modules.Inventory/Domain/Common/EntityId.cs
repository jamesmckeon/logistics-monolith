using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Domain.Common;

public sealed class EntityId : ValueObject
{
    private EntityId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static EntityId Create()
    {
        return new EntityId(Guid.CreateVersion7());
    }

    // Reconstitutes an EntityId from an already-assigned value (e.g. materializing from the database).
    // Unlike Create(), this does not generate a new identity.
    public static EntityId FromGuid(Guid value)
    {
        return new EntityId(value);
    }
}