using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Skus;

internal sealed class OwnerSku : ValueObject
{
    private OwnerSku(
        int ownerId, UpperCaseString skuCode, Guid id, bool lotTracked, bool expirationTracked)
    {
        OwnerId = ownerId;
        SkuCode = skuCode;
        Id = id;
        IsLotTracked = lotTracked;
        IsExpirationTracked = expirationTracked;
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