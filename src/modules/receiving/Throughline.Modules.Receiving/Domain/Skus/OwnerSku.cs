using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Skus;

internal sealed class OwnerSku : ValueObject
{
    public OwnerSku(
        int ownerId, UpperCaseString skuCode, Guid id, bool isLotTracked, bool isExpirationTracked)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("id cannot be empty", nameof(id));
        }

        OwnerId = ownerId;
        SkuCode = skuCode;
        Id = id;
        IsLotTracked = isLotTracked;
        IsExpirationTracked = isExpirationTracked;
    }

    public int OwnerId { get; }
    public UpperCaseString SkuCode { get; }
    public Guid Id { get; }
    public bool IsLotTracked { get; }
    public bool IsExpirationTracked { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Id;
    }
}