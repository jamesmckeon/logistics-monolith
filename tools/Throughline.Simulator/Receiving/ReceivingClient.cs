using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Throughline.Simulator.Ledger;

namespace Throughline.Simulator.Receiving;

/// <summary>
///     A receiver recording an arrived trailer as an unplanned delivery (<c>POST /receiving/receipts</c>).
///     Defined here rather than shared with Throughline, as an outside caller's would be.
/// </summary>
internal sealed class ReceivingClient
{
    private const string OwnerIdHeader = "owner_id";

    private readonly HttpClient _http;

    public ReceivingClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ReceiptResponse> ReceiveAsync(
        InboundShipment shipment, Guid operatorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        // the trailer's id is the receipt id, so a resend is recognized as the same delivery
        var body = new ReceiptRequest(
            shipment.ShipmentId,
            operatorId,
            shipment.DeliveryReference,
            shipment.BillOfLading,
            shipment.CarrierScac,
            shipment.TrailerNumber,
            "",
            shipment.ShipperName,
            shipment.Pallets.Select(p => new ReceiptPallet(p.Lpn, p.Sku, p.Quantity, p.LocationId)).ToList());

        using var request = new HttpRequestMessage(HttpMethod.Post, "receiving/receipts");
        request.Content = JsonContent.Create(body);
        request.Headers.Add(OwnerIdHeader, shipment.OwnerId.ToString(CultureInfo.InvariantCulture));

        var started = Stopwatch.GetTimestamp();
        using var response = await _http.SendAsync(request, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started);

        ReceiptResult? result = null;

        if (response.StatusCode == HttpStatusCode.OK)
            result = await response.Content.ReadFromJsonAsync<ReceiptResult>(cancellationToken);

        return new ReceiptResponse(response.StatusCode, result, elapsed);
    }

    private sealed record ReceiptRequest(
        Guid ReceiptId,
        Guid OperatorId,
        string DeliveryReference,
        string BillOfLading,
        string CarrierScac,
        string TrailerNumber,
        string ContainerNumber,
        string ShipperName,
        IReadOnlyList<ReceiptPallet> Pallets);

    private sealed record ReceiptPallet(string Lpn, string Sku, int Quantity, string LocationId);
}

internal sealed record ReceiptResult(string ReceiptNumber, IReadOnlyList<ReceiptPalletResult> Pallets);

/// <param name="Outcome">
///     <c>received_available</c>, <c>received_on_hold</c> or <c>receiving_exception</c>
/// </param>
internal sealed record ReceiptPalletResult(string Lpn, string Outcome);

internal sealed record ReceiptResponse(HttpStatusCode StatusCode, ReceiptResult? Result, TimeSpan Elapsed);
