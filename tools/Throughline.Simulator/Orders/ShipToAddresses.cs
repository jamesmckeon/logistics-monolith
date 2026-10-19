namespace Throughline.Simulator.Orders;

/// <summary>
///     Made-up consumer ship-to addresses in real cities with valid ZIP codes
/// </summary>
internal static class ShipToAddresses
{
    public static readonly IReadOnlyList<(string Street, string? Street2, string City, string State, string PostalCode)>
        All =
        [
            ("1420 Larkspur Lane", null, "Minneapolis", "MN", "55408"),
            ("88 Quarry Road", "Apt 3B", "Denver", "CO", "80205"),
            ("2715 Hollow Creek Drive", null, "Austin", "TX", "78745"),
            ("640 Birchwood Avenue", null, "Madison", "WI", "53703"),
            ("19 Saltmarsh Court", "Unit 12", "Charleston", "SC", "29412"),
            ("3302 Timberline Way", null, "Boise", "ID", "83706"),
            ("510 Granite Street", "Suite 200", "Portland", "OR", "97211"),
            ("77 Cedar Bluff Road", null, "Knoxville", "TN", "37923"),
            ("905 Maple Terrace", null, "Columbus", "OH", "43215"),
            ("1188 Summit Ridge", "Apt 7", "Salt Lake City", "UT", "84105")
        ];
}
