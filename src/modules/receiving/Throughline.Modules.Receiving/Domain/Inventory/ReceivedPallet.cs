using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed class ReceivedPallet : Entity<string>
{
    private ReceivedPallet(
        string id,
        string licensePlateNumber,
        string skuCode,
        int quantity,
        ReceivingLocation location,
        HoldReasons holdReasons,
        IEnumerable<ReceivingExceptions> receivingExceptions)

    public UpperCaseString LicensePlateNumber { get; }
    public UpperCaseString SkuCode { get; }
    public int Quantity { get; }
    public ReceivingLocation Location { get; }
    public HoldReasons? HoldReason { get; }
    public IReadOnlyCollection<ReceivingExceptions> Exceptions { get; }
}