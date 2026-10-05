using Throughline.Modules.Receiving.Application.Common;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Domain.Skus;

namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed record PalletServiceRequest(
    int OwnerId,
    SubmittedPallet[] Pallets,
    OwnerSku[] OwnerSkus,
    HoldReason[] HoldReasons,
    DefaultLocations DefaultLocations,
    ReceivedLpn[] ExistingLpns,
    ReceivingLocation[] Locations
);