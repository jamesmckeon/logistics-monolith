using System.Globalization;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Ledger;
using Throughline.Simulator.Scheduling;

namespace Throughline.Simulator.Buying;

/// <summary>
///     How the supplier ships a purchase order: single-SKU pallets, loaded onto trailers that each
///     arrive during dock hours after the lead time
/// </summary>
internal sealed class ShipmentPlanner
{
    private static readonly (int Min, int Max) OpeningLeadTimeDays = (0, 1);
    private static readonly (int Min, int Max) LateByDays = (1, 3);

    private readonly BuyingProfile _buying;
    private readonly DockCalendar _dock;
    private readonly FacilityOptions _facility;
    private readonly Random _random;

    public ShipmentPlanner(BuyingProfile buying, FacilityOptions facility, DockCalendar dock, Random random)
    {
        ArgumentNullException.ThrowIfNull(buying);
        ArgumentNullException.ThrowIfNull(facility);
        ArgumentNullException.ThrowIfNull(dock);
        ArgumentNullException.ThrowIfNull(random);

        _buying = buying;
        _facility = facility;
        _dock = dock;
        _random = random;
    }

    public PurchaseOrder Plan(
        int ownerId, string referencePrefix, PurchaseOrderKind kind, IReadOnlyList<PurchaseLine> lines,
        DateTimeOffset orderedAt)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var purchaseOrderNumber = string.Create(CultureInfo.InvariantCulture,
            $"{referencePrefix}-PO-{orderedAt.UtcDateTime:yyyyMMdd}-{_random.Next(0x10000):X4}");

        var pallets = lines.SelectMany(PalletsFor).ToList();

        var shipments = pallets
            .Chunk(_buying.PalletsPerTrailer)
            .Select((trailer, index) => new InboundShipment(
                Guid.NewGuid(),
                ownerId,
                string.Create(CultureInfo.InvariantCulture, $"{purchaseOrderNumber}-{index + 1:D2}"),
                string.Create(CultureInfo.InvariantCulture, $"BOL{_random.NextInt64(10_000_000_000):D10}"),
                _facility.CarrierScacs[_random.Next(_facility.CarrierScacs.Length)],
                string.Create(CultureInfo.InvariantCulture, $"TRL{_random.Next(100_000):D5}"),
                _buying.SupplierName,
                DueAt(kind, orderedAt),
                trailer))
            .ToList();

        return new PurchaseOrder(Guid.NewGuid(), ownerId, purchaseOrderNumber, kind, orderedAt, shipments);
    }

    private IEnumerable<InboundPallet> PalletsFor(PurchaseLine line)
    {
        for (var remaining = line.Units; remaining > 0; remaining -= _buying.UnitsPerPallet)
        {
            yield return new InboundPallet(
                ("LPN" + Guid.NewGuid().ToString("N"))[..15].ToUpperInvariant(),
                line.Sku,
                Math.Min(remaining, _buying.UnitsPerPallet),
                string.Create(CultureInfo.InvariantCulture,
                    $"{_facility.PutawayLocationPrefix}{_random.Next(1, _facility.PutawayLocationCount + 1):D2}"));
        }
    }

    private DateTimeOffset DueAt(PurchaseOrderKind kind, DateTimeOffset orderedAt)
    {
        var (min, max) = kind == PurchaseOrderKind.Opening
            ? OpeningLeadTimeDays
            : (_buying.LeadTimeDaysMin, _buying.LeadTimeDaysMax);
        var days = _random.Next(min, max + 1);

        if (_random.NextDouble() < _buying.LateShipmentRate)
            days += _random.Next(LateByDays.Min, LateByDays.Max + 1);

        var orderedOn = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(orderedAt, _dock.Zone).DateTime);

        return _dock.ArrivalOn(orderedOn.AddDays(days), _random);
    }
}
