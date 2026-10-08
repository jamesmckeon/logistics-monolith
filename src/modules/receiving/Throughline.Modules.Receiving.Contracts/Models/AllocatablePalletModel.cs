namespace Throughline.Modules.Receiving.Contracts.Models;

public sealed record AllocatablePalletModel(
    string Lpn,
    string SkuCode,
    int Quantity,
    string LocationId
);