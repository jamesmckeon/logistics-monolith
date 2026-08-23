namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class AddressState : ValueObject
{
    public AddressState(string value)
    {
        if (GetValidationErrors(value).Any())
            throw new ArgumentException("value is not a valid state name", nameof(value));

        Value = value;
    }

    public string Value { get; }

    public static Result<AddressState> Create(string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        var errors = GetValidationErrors(state);
        
        return errors.Any() ? Result<AddressState>.Validation(errors): 
            Result<AddressState>.Success(new AddressState(state));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    private static Error[] GetValidationErrors(string state)
    {
        if (state.Trim().Length != 2 || state.Trim().Any(a => !char.IsLetter(a)))
            return [new Error("state must be 2 alpha characters", "state")];

        return [];
    }
}