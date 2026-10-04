using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps the ReceiptNumber value object to its underlying string column.
// Registered once per DbContext via ConfigureConventions so every ReceiptNumber property persists consistently.
// EF never passes null to a value converter, so nullable ReceiptNumber properties use it too.
internal sealed class ReceiptNumberValueConverter : ValueConverter<ReceiptNumber, string>
{
    public ReceiptNumberValueConverter()
        : base(
            receiptNumber => receiptNumber.Value,
            value => new ReceiptNumber(value))
    {
    }
}