using Throughline.Modules.Ordering.Domain.Exceptions;

namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class StreetAddress
{
    public StreetAddress(
        string streeAddressOne,
        string? streetAddressTwo,
        string city,
        AddressState state,
        PostalCode zipCode)
    {
        if (GetValidationErrors(streeAddressOne,
                streetAddressTwo, city, state, zipCode).Any())
            throw new DomainValidationException();

        StreeAddressOne = streeAddressOne;
        StreetAddressTwo = streetAddressTwo;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public string StreeAddressOne { get; }
    public string? StreetAddressTwo { get; }
    public string City { get; }
    public AddressState State { get; }
    public PostalCode ZipCode { get; }

    public static Result<StreetAddress> Create(
        string addressOne,
        string? addressTwo,
        string city,
        AddressState state,
        PostalCode postalCode)
    {
        var errors = GetValidationErrors(addressOne, addressTwo, city, state, postalCode);

        return errors.Any()
            ? errors
            : new StreetAddress(
                addressOne.Trim(),
                addressTwo?.Trim(),
                city.Trim(),
                state,
                postalCode);
    }

    private static Error[] GetValidationErrors(
        string addressOne,
        string? addressTwo,
        string city,
        AddressState state,
        PostalCode postalCode)
    {
        ArgumentNullException.ThrowIfNull(addressOne);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(postalCode);

        var errors = new List<Error>();

        if (addressOne.Trim() == "")
            errors.Add(Error.Validation("addressOne is required"));

        if (city.Trim() == "")
            errors.Add(Error.Validation("city is required"));

        return errors.ToArray();
    }
}