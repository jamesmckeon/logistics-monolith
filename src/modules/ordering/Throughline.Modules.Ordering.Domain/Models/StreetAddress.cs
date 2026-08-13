namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class StreetAddress
{
    public StreetAddress(
        string streeAddressOne,
        string? streetAddressTwo,
        string city,
        string state,
        PostalCode zipCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streeAddressOne);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentNullException.ThrowIfNull(zipCode);

        if (state.Trim().Length != 2 || state.Any(c => char.IsDigit(c)))
            throw new ArgumentException("state must be two alpha characters");

        StreeAddressOne = streeAddressOne;
        StreetAddressTwo = streetAddressTwo;
        City = city;
        State = state.Trim().ToUpperInvariant();
        ZipCode = zipCode;
    }

    public string StreeAddressOne { get; }
    public string? StreetAddressTwo { get; }
    public string City { get; }
    public string State { get; }
    public PostalCode ZipCode { get; }
}