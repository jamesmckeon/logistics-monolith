using Throughline.Common.Models;

namespace Throughline.Modules.Receiving.Domain.Common;

internal sealed class ReceiptNumber : ValueObject
{
    private ReceiptNumber(string value, AppDateTime createdOn)
    {
        Value = value;
        CreatedOn = createdOn;
    }

    public string Value { get; }
    public AppDateTime CreatedOn { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public static ReceiptNumber Create(ReceiptNumber lastReceiptNumber)
    {
        var number = int.Parse(lastReceiptNumber.Value.Split('-')[2]);
        return new ReceiptNumber($"RCPT-{(number + 1).ToString().PadLeft(10, '0')}", AppDateTime.Now);
    }
}