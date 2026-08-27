namespace Throughline.Modules.Billing.Application.Orders.Models;

public sealed record DestinationModel(
    string StreetAddressOne,
    string? StreetAddressTwo,
    string City,
    string State,
    string PostalCode);