using System.Text.Json.Serialization;
using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed class PalletError
{
    // Also the deserialization constructor for replaying a stored result
    [JsonConstructor]
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