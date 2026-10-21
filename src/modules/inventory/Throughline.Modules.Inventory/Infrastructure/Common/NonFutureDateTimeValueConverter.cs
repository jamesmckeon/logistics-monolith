using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Common.Models;

namespace Throughline.Modules.Inventory.Infrastructure.Common;

// Maps the NonFutureDateTime value object to its underlying DateTimeOffset (timestamptz) column.
// EF never passes null to a value converter, so nullable NonFutureDateTime properties can use it too.
internal sealed class NonFutureDateTimeValueConverter : ValueConverter<NonFutureDateTime, DateTimeOffset>
{
    public NonFutureDateTimeValueConverter()
        : base(
            dateTime => dateTime.Value,
            value => new NonFutureDateTime(value))
    {
    }
}
