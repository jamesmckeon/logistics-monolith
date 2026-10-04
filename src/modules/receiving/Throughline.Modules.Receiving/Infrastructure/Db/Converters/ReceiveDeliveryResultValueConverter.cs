using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

namespace Throughline.Modules.Receiving.Infrastructure.Db.Converters;

// Maps a ReceiveDeliveryResult to the JSON document it is replayed from when a client resubmits a delivery.
// Serialization goes through static methods because a converter's lambdas are expression trees, which can't
// call JsonSerializer's methods with their optional options parameter omitted.
internal sealed class ReceiveDeliveryResultValueConverter : ValueConverter<ReceiveDeliveryResult, string>
{
    public ReceiveDeliveryResultValueConverter()
        : base(
            result => Serialize(result),
            json => Deserialize(json))
    {
    }

    private static string Serialize(ReceiveDeliveryResult result) => JsonSerializer.Serialize(result);

    private static ReceiveDeliveryResult Deserialize(string json) =>
        JsonSerializer.Deserialize<ReceiveDeliveryResult>(json)
        ?? throw new InvalidOperationException("A stored delivery result deserialized to null");
}
