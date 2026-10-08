namespace Throughline.Modules.Receiving.Domain.Inventory;

internal enum ReceivingExceptions
{
    InvalidSkuCode,
    InvalidLocation,
    ExistingLpn,
    InvalidHoldReason,
    LotNumberRequired,
    ExpirationDateRequired
}