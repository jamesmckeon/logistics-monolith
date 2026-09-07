using Throughline.Common.Models;

namespace Throughline.Modules.Ordering.Domain.Orders;

internal sealed class OwnerReferenceNumber : ValueObject
{
    internal OwnerReferenceNumber(int ownerId, string referenceNumber)
    {
        ArgumentNullException.ThrowIfNull(referenceNumber);
        OwnerId = ownerId;
        ReferenceNumber = referenceNumber;
    }

    public int OwnerId { get; }
    public string ReferenceNumber { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return OwnerId;
        yield return ReferenceNumber;
    }
}