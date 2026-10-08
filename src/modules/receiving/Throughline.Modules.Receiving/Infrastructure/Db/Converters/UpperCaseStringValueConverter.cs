using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps the UpperCaseString value object to its underlying string column.
// Registered once per DbContext via ConfigureConventions so every UpperCaseString property persists consistently.
// EF never passes null to a value converter, so nullable UpperCaseString properties use it too.
internal sealed class UpperCaseStringValueConverter : ValueConverter<UpperCaseString, string>
{
    public UpperCaseStringValueConverter()
        : base(
            upperCaseString => upperCaseString.Value,
            value => new UpperCaseString(value))
    {
    }
}