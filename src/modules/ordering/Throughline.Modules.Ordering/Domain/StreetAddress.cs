using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Ordering.Domain;

public sealed class StreetAddress : ValueObject
{
    internal StreetAddress(
        string streeAddressOne,
        string? streetAddressTwo,
        string city,
        string state,
        PostalCode zipCode)
    {
        ArgumentNullException.ThrowIfNull(streeAddressOne);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(zipCode);

        StreeAddressOne = streeAddressOne;
        StreetAddressTwo = streetAddressTwo;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public string StreeAddressOne { get; }
    public string? StreetAddressTwo { get; }
    public string City { get; }
    public string State { get; }
    public PostalCode ZipCode { get; }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return StreeAddressOne;
        yield return StreetAddressTwo ?? string.Empty;
        yield return City;
        yield return State;
        yield return ZipCode;
    }

    internal static Result<StreetAddress> Create(
        string addressOne,
        string? addressTwo,
        string city,
        string state,
        PostalCode postalCode)
    {
        ArgumentNullException.ThrowIfNull(addressOne);
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(postalCode);

        var errors = new List<Error>();

        if (addressOne.Trim() == "")
            errors.Add(Error.IsRequired(nameof(addressOne)));

        if (city.Trim() == "")
            errors.Add(Error.IsRequired(nameof(city)));

        if (state.Trim() == "")
        {
            errors.Add(Error.IsRequired(nameof(state)));
        }
        else
        {
            if (state.Trim().Length != 2 || state.Trim().Any(a => !char.IsLetter(a)))
                errors.Add(new Error("state must be 2 alpha characters", "state"));
        }


        return errors.Any()
            ? Result<StreetAddress>.Validation(errors)
            : new StreetAddress(addressOne, addressTwo, city, state, postalCode);
    }
}