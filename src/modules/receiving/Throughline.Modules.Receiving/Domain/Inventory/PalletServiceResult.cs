namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed record PalletServiceResult(
    IReadOnlyCollection<Pallet> ValidPallets,
    IReadOnlyCollection<InvalidPallet> InvalidPallets);