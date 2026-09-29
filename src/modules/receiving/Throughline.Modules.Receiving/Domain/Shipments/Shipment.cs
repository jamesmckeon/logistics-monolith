using Throughline.Modules.Receiving.Shipments;

namespace Throughline.Modules.Receiving.Domain.Shipments;

internal sealed record Shipment(
    CarrierScac CarrierScac,
    string BillOfLading,
    string ShipperName,
    string TrailerNumber,
    string ContainerNumber,
    string ShipperReference)
{
}