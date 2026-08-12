using System.Text.RegularExpressions;

namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class PostalCode : ValueObject
{
    internal const int MinValue = 501;
    internal const int MaxValue = 99950;

    // Matches 5 digits OR 5 digits followed by a hyphen and 4 digits
    private static readonly Regex UsZipRegex = new(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

    public PostalCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!IsValid(value))
            throw new FormatException($"'{value}' is not a valid postal code format");

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

    public static bool IsValid(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return false;

        if (!UsZipRegex.IsMatch(zipCode.Trim()))
            return false;

        var parts = Parse(zipCode);

        if (parts.Left <= 5000 || parts.Left >= 99501)
            return false;

        if (parts.Right is < 1)
            return false;

        return true;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }


    public override string ToString()
    {
        return Value;
    }
}