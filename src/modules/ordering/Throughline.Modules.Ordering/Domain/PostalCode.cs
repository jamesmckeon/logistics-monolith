using System.Text.RegularExpressions;
using Throughline.Common.Models;
using Throughline.Common.Results;

namespace Throughline.Modules.Billing.Domain.Models;

public sealed class PostalCode : ValueObject
{
    // Matches 5 digits OR 5 digits followed by a hyphen and 4 digits
    private static readonly Regex UsZipRegex = new(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

    internal PostalCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public string Value { get; }

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
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        var errors = new List<Error>();

        if (!UsZipRegex.IsMatch(trimmed))

        {
            errors.Add(new Error("Invalid postal code format"));
        }
        else
        {
            var parts = Parse(trimmed);

            if (parts.Left <= 00500 || parts.Left >= 99501)
                errors.Add(
                    new Error("The start of a postal code must be between 00501 and 99500"));

            if (parts.Right.HasValue && parts.Right < 1)
                errors.Add(
                    new Error("The last four of a postal code must be greater than 0000"));
        }

        return errors.Any() ? Result<PostalCode>.Validation(errors) : new PostalCode(value);
    }
}