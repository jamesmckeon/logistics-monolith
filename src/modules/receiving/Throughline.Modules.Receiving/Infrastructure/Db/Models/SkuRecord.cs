namespace Throughline.Modules.Receiving.Infrastructure.Db.Models;

/// <summary>
///     A row of Receiving's SKU master. A persistence record rather than <see cref="Domain.Skus.OwnerSku" />,
///     which EF already maps as a complex type within a pallet and so can't also be an entity.
/// </summary>
internal sealed class SkuRecord
{
    public Guid SkuId { get; init; }
    public int OwnerId { get; init; }
    public required string SkuCode { get; init; }
    public bool IsLotTracked { get; init; }
    public bool IsExpirationTracked { get; init; }
}
