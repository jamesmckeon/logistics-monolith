using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps the ScacCode value object to its underlying string column.
// Registered once per DbContext via ConfigureConventions so every ScacCode property persists consistently.
// EF never passes null to a value converter, so nullable ScacCode properties use it too.
internal sealed class ScacCodeValueConverter : ValueConverter<ScacCode, string>
{
    public ScacCodeValueConverter()
        : base(
            scacCode => scacCode.Value,
            value => new ScacCode(value))
    {
    }
}