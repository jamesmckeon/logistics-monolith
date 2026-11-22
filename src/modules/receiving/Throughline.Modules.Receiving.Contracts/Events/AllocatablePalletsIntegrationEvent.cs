using Throughline.Common.Events;
using Throughline.Modules.Receiving.Contracts.Models;

namespace Throughline.Modules.Receiving.Contracts.Events;

/// <param name="CreatedBy">The Entra object ID (<c>oid</c>) of the caller who received the delivery</param>
public sealed record AllocatablePalletsIntegrationEvent(
    int OwnerId,
    Guid ReceiptId,
    string ReceiptNumber,
    DateTimeOffset ReceivedOn,
    IReadOnlyCollection<AllocatablePalletModel> Pallets,
    Guid? CreatedBy) : IntegrationEventBase(CreatedBy);
