namespace Throughline.Modules.Receiving.Infrastructure.Db.Models;

/// <summary>
///     A row of Receiving's carrier master. A persistence record rather than
///     <see cref="Domain.Shipments.CarrierScac" />, which EF already maps as a complex type within a shipment and
///     so can't also be an entity.
/// </summary>
internal sealed class CarrierRecord
{
    public int CarrierId { get; init; }
    public required string ScacCode { get; init; }
    public required string CarrierName { get; init; }
}
