using Throughline.Modules.Receiving.Application.Models;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed class PalletError(string Code, string Description)
{
    public static PalletError UnrecognizedSku(string sku)
    {
        return new PalletError(PalletErrorCodes.UnknownSku, $"SKU code '{sku}' is not in the system");
    }
    
    public static PalletError DamagedPallet()
    {
        return new PalletError(PalletErrorCodes.Damaged, $"Pallet was received damaged");
    }
}