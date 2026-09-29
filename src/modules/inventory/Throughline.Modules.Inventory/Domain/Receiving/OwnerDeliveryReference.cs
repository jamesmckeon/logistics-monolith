using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Domain.Receiving;

internal sealed class OwnerDeliveryReference : ValueObject
{
    private OwnerDeliveryReference(int ownerId, string referenceNumber)
    {
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

    public static Result<OwnerDeliveryReference> Create(EntityId id, int ownerId, string referenceNumber)
    {
        if (referenceNumber.Trim() == "")
        {
            return Result<OwnerDeliveryReference>.Validation("referenceNumber is required");
        }

        return new OwnerDeliveryReference(ownerId, referenceNumber);
    }
}