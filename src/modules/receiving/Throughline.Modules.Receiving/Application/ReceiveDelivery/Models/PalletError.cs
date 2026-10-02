using Throughline.Modules.Receiving.Application.Models;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed class PalletError(string Code, string Description)
{
    public static PalletError UnrecognizedSku(string sku) =>
        new(PalletErrorCodes.UnknownSku, $"SKU code '{sku}' is not in the system");

    public static PalletError DamagedPallet() => new(PalletErrorCodes.Damaged, "Pallet was received damaged");
}