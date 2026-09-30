using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Shipments;

/// <summary>
///     A shipment for which there isn't a carrier and/or scac code in
///     the system yet
/// </summary>
internal sealed record InvalidShipment(
    TrimmedString CarrierName,
    ScacCode ScacCode,
    TrimmedString BillOfLading,
    TrimmedString ShipperName,
    TrimmedString TrailerNumber,
    TrimmedString ContainerNumber,
    TrimmedString ShipperReference)
{
}