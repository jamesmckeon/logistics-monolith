using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps the NonFutureDateTime value object to its underlying DateTimeOffset column.
// Registered once per DbContext via ConfigureConventions so every NonFutureDateTime property persists consistently.
// EF never passes null to a value converter, so nullable NonFutureDateTime properties use it too.
internal sealed class NonFutureDateTimeValueConverter : ValueConverter<NonFutureDateTime, DateTimeOffset>
{
    public NonFutureDateTimeValueConverter()
        : base(
            dateTime => dateTime.Value,
            value => new NonFutureDateTime(value))
    {
    }
}