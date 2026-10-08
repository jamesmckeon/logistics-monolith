namespace Throughline.Modules.Receiving.Domain.Inventory;

internal interface IPalletService
{
    PalletServiceResult BuildPallets(PalletServiceRequest request);
}