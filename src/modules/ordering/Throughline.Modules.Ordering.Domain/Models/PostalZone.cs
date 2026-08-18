namespace Throughline.Modules.Ordering.Domain.Models;

public sealed class PostalZone : ValueObject
{
    public PostalZone(PostalCode startCode, PostalCode endCode)
    {
        ArgumentNullException.ThrowIfNull(startCode);
        ArgumentNullException.ThrowIfNull(endCode);

        if (int.Parse(startCode.BaseCode) > int.Parse(endCode.BaseCode))
            throw new InvalidOperationException("Start must be lesser or equal to end");

        StartCode = startCode;
        EndCode = endCode;
    }

    public PostalCode StartCode { get; }
    public PostalCode EndCode { get; }

    public bool Includes(PostalCode postalCode)
    {
        ArgumentNullException.ThrowIfNull(postalCode);

        var candidate = int.Parse(postalCode.BaseCode);
        var start = int.Parse(StartCode.BaseCode);
        var end = int.Parse(EndCode.BaseCode);

        return candidate >= start && candidate <= end;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return StartCode;
        yield return EndCode;
    }
}