using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps the TrimmedString value object to its underlying string column.
// Registered once per DbContext via ConfigureConventions so every TrimmedString property persists consistently.
// EF never passes null to a value converter, so nullable TrimmedString properties use it too.
internal sealed class TrimmedStringValueConverter : ValueConverter<TrimmedString, string>
{
    public TrimmedStringValueConverter()
        : base(
            trimmedString => trimmedString.Value,
            value => new TrimmedString(value))
    {
    }
}