using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps the UniqueId value object to its underlying Guid column.
// Registered once per DbContext via ConfigureConventions so every UniqueId property persists consistently.
// EF never passes null to a value converter, so nullable UniqueId properties use it too.
internal sealed class UniqueIdValueConverter : ValueConverter<UniqueId, Guid>
{
    public UniqueIdValueConverter()
        : base(
            id => id.Value,
            value => new UniqueId(value))
    {
    }
}