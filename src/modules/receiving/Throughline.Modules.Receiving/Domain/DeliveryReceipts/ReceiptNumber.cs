using System.Text.RegularExpressions;
using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.DeliveryReceipts;

internal sealed class ReceiptNumber : ValueObject
{
    /// <summary>
    ///     The receipt number prefix
    /// </summary>
    public const string Prefix = "RCPT-";

    /// <summary>
    ///     The total length of a receipt number
    /// </summary>
    public const int Length = 15;

    /// <param name="value">
    ///     A receipt number in the form <c>RCPT-</c> followed by exactly 10 digits, e.g. <c>RCPT-0000000042</c>;
    ///     upper case, with no surrounding whitespace
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="value" /> is not in the expected format</exception>
    public ReceiptNumber(string value)
    {
        // [0-9] rather than \d, which also matches non-ASCII digits; \z rather than $, which allows a trailing newline
        if (!Regex.IsMatch(value, @"^RCPT-[0-9]{10}\z"))
        {
            throw new ArgumentException("value must be RCPT- followed by 10 digits", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static ReceiptNumber Create(ReceiptNumber? lastReceiptNumber)
    {
        var number = 0;

        if (lastReceiptNumber is not null)
        {
            number = int.Parse(lastReceiptNumber.Value.Split('-')[1]);
        }

        return new ReceiptNumber($"RCPT-{(number + 1).ToString().PadLeft(10, '0')}");
    }
}