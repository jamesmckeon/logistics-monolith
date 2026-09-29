using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Infrastructure.Common;

// Maps the AppDateTime value object to its underlying DateTimeOffset (timestamptz) column.
// EF never passes null to a value converter, so nullable AppDateTime properties can use it too.
internal sealed class AppDateTimeValueConverter : ValueConverter<AppDateTime, DateTimeOffset>
{
    public AppDateTimeValueConverter()
        : base(
            dateTime => dateTime.Value,
            value => new AppDateTime(value))
    {
    }
}
