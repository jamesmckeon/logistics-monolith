namespace Throughline.Modules.Ordering.Application.Orders;

public sealed class DestinationRequest
{
    internal DestinationRequest(
        string streetAddressOne,
        string? streetAddressTwo,
        string locality,
        string region,
        string postalCode,
        string countryCode)
    {
        StreetAddressOne = streetAddressOne;
        StreetAddressTwo = streetAddressTwo;
        Locality = locality;
        Region = region;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    public string StreetAddressOne { get; }
    public string? StreetAddressTwo { get; }
    public string Locality { get; }
    public string Region { get; }
    public string PostalCode { get; }
    public string CountryCode { get; }
}