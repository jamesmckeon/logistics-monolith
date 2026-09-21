using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Skus;

internal sealed class Sku : Entity<EntityId>
{
    public Sku(EntityId id, int ownerId, string code) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        OwnerId = ownerId;
        Code = code;
    }

    public string Code { get; }
    public int OwnerId { get; }

    public override string ToString()
    {
        return Code;
    }
}