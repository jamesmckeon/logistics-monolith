using Throughline.Modules.Receiving.Contracts.Models;

namespace Throughline.Modules.Receiving.Contracts.Events;

public sealed record AllocatablePalletsIntegrationEvent(
    int OwnerId,
    Guid ReceiptId,
    string ReceiptNumber,
    DateTimeOffset ReceivedOn,
    IReadOnlyCollection<AllocatablePalletModel> Pallets)
{
}