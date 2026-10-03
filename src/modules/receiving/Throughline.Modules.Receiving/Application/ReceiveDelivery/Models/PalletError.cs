using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed class PalletError
{
    private PalletError(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public string Code { get; }
    public string Description { get; }

    public static PalletError Exception(ReceivingExceptions exception) =>
        new("PALLET_EXCEPTION", exception.ToString());

    public static PalletError OnHold(HoldReason holdReason) =>
        new("PALLET_ON_HOLD", $"Reason code = {holdReason}");
}