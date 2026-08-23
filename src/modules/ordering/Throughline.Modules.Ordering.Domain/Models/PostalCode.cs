using System.Text.RegularExpressions;

namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class PostalCode : ValueObject
{
    // Matches 5 digits OR 5 digits followed by a hyphen and 4 digits
    private static readonly Regex UsZipRegex = new(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

    public PostalCode(string value)
    {
        if (Validate(value).Any())
            throw new ArgumentException("value is invalid", nameof(value));

        Value = value.Trim();
        BaseCode = value.Split("-", StringSplitOptions.TrimEntries)[0];
    }

    public string Value { get; }
    public string BaseCode { get; }

    private static (int Left, int? Right) Parse(string postalCode)
    {
        var parts = postalCode.Split("-");

        return (int.Parse(parts[0]), parts.Length == 2 ? int.Parse(parts[1]) : null);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }


    public override string ToString()
    {
        return Value;
    }

    public static Result<PostalCode> Create(string value)
    {
        var errors = Validate(value);
        return errors.Any() ? Result<PostalCode>.Validation(errors) : new PostalCode(value);
    }

    private static Error[] Validate(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        var errors = new List<Error>();

        if (!UsZipRegex.IsMatch(trimmed))
        {
            errors.Add(new Error("Invalid postal code format"));
            return errors.ToArray();
        }

        var parts = Parse(trimmed);

        if (parts.Left <= 00500 || parts.Left >= 99501)
            errors.Add(
                new Error("The start of a postal code must be between 00501 and 99500"));

        if (parts.Right.HasValue && parts.Right < 1)
            errors.Add(
                new Error("The last four of a postal code must be greater than 0000"));

        return errors.ToArray();
    }
}