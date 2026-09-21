using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Infrastructure.Common;

// Maps the EntityId value object to its underlying Guid (uuid) column.
// Registered once per DbContext via ConfigureConventions so every EntityId property
// — keys and foreign keys alike — persists consistently without per-property setup.
internal sealed class EntityIdValueConverter : ValueConverter<EntityId, Guid>
{
    public EntityIdValueConverter()
        : base(
            id => id.Value,
            value => EntityId.FromGuid(value))
    {
    }
}
