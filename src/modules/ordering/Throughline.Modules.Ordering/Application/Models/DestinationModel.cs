using Throughline.Modules.Ordering.Domain;

namespace Throughline.Modules.Ordering.Application.Models;

public sealed record DestinationModel(
    string StreetAddressOne,
    string? StreetAddressTwo,
    string City,
    string State,
    string PostalCode)
{
    internal static DestinationModel FromStreetAddress(StreetAddress streetAddress)
    {
        return new DestinationModel(streetAddress.StreeAddressOne, streetAddress.StreetAddressTwo, streetAddress.City,
            streetAddress.State, streetAddress.ZipCode.Value);
    }
}