using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Receipts;

internal sealed class SkuReceipt
{
    public SkuReceipt(OwnerSku ownerSku, int quantity, AppDateTime receivedOn)
    {
        ArgumentNullException.ThrowIfNull(ownerSku);
        ArgumentNullException.ThrowIfNull(receivedOn);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        OwnerSku = ownerSku;
        Quantity = quantity;
        ReceivedOn = receivedOn;
    }

    public OwnerSku OwnerSku { get; }
    public int Quantity { get; }
    public AppDateTime ReceivedOn { get; }
}