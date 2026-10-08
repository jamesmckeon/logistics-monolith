using System.Diagnostics.CodeAnalysis;
using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Shipments;

internal sealed class Shipment : ValueObject
{
    [SetsRequiredMembers]
    public Shipment(
        CarrierScac carrierScac,
        TrimmedString? billOfLading,
        TrimmedString? shipperName,
        TrimmedString? trailerNumber,
        TrimmedString? containerNumber,
        TrimmedString shipperReference)
        : this(billOfLading, shipperName, trailerNumber, containerNumber, shipperReference)
    {
        CarrierScac = carrierScac;
    }

    // EF materialization constructor; EF can't pass complex values (CarrierScac) to a constructor,
    // so it sets CarrierScac after construction
    private Shipment(
        TrimmedString? billOfLading,
        TrimmedString? shipperName,
        TrimmedString? trailerNumber,
        TrimmedString? containerNumber,
        TrimmedString shipperReference)
    {
        if (trailerNumber is null && containerNumber is null)
        {
            throw new InvalidOperationException("Either trailerNumber or containerNumber is required");
        }

        ShipperName = shipperName;
        TrailerNumber = trailerNumber;
        ContainerNumber = containerNumber;
        ShipperReference = shipperReference;
        BillOfLading = billOfLading;
    }

    public required CarrierScac CarrierScac { get; init; }
    public TrimmedString? BillOfLading { get; }
    public TrimmedString? ShipperName { get; }
    public TrimmedString? TrailerNumber { get; }
    public TrimmedString? ContainerNumber { get; }
    public TrimmedString ShipperReference { get; }


    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return CarrierScac;
        yield return BillOfLading;
        yield return ShipperName;
        yield return TrailerNumber;
        yield return ContainerNumber;
        yield return ShipperReference;
    }
}