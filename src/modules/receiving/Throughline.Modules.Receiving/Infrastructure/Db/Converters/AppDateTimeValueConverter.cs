using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps the AppDateTime value object to its underlying DateTimeOffset column.
// Registered once per DbContext via ConfigureConventions so every AppDateTime property persists consistently.
// EF never passes null to a value converter, so nullable AppDateTime properties use it too.
internal sealed class AppDateTimeValueConverter : ValueConverter<AppDateTime, DateTimeOffset>
{
    public AppDateTimeValueConverter()
        : base(
            dateTime => dateTime.Value,
            value => new AppDateTime(value))
    {
    }
}