using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Domain.Inventory;

internal sealed class SkuReceipt : Entity<Guid>
{
    public SkuReceipt(Sku sku, int quantityReceived, AppDateTime receivedOn)
        : base(Guid.CreateVersion7())
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityReceived);
        ArgumentNullException.ThrowIfNull(receivedOn);

        Sku = sku;
        QuantityReceived = quantityReceived;
        ReceivedOn = receivedOn;
    }

    public Sku Sku { get; }
    public int QuantityReceived { get; }
    public AppDateTime ReceivedOn { get; }
}