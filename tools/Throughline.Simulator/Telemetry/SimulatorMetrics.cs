using System.Diagnostics;
using System.Diagnostics.Metrics;
using Throughline.Simulator.Orders;

namespace Throughline.Simulator.Telemetry;

internal sealed class SimulatorMetrics
{
    public const string MeterName = "Throughline.Simulator";

    private readonly Histogram<double> _orderDuration;
    private readonly Counter<long> _ordersSent;
    private readonly Counter<long> _palletsSent;
    private readonly Histogram<double> _receiptDuration;
    private readonly Counter<long> _receiptsSent;

    public SimulatorMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(MeterName);
        _ordersSent = meter.CreateCounter<long>(
            "simulator.orders.sent", "{order}", "940s sent to Throughline, by kind and outcome");
        _orderDuration = meter.CreateHistogram<double>(
            "simulator.orders.duration", "ms", "Time to send a 940, including retries");
        _receiptsSent = meter.CreateCounter<long>(
            "simulator.receipts.sent", "{receipt}", "Trailers sent to Throughline as receipts, by outcome");
        _palletsSent = meter.CreateCounter<long>(
            "simulator.receipts.pallets", "{pallet}", "Pallets on trailers sent as receipts");
        _receiptDuration = meter.CreateHistogram<double>(
            "simulator.receipts.duration", "ms", "Time to send a receipt, including retries");
    }

    /// <param name="status">The HTTP status, or null when no response arrived</param>
    public void OrderSent(string customer, OrderKind kind, int? status, bool asExpected, TimeSpan elapsed)
    {
        var tags = new TagList
        {
            { "customer", customer },
            { "kind", kind.ToString() },
            { "status", status?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none" },
            { "as_expected", asExpected }
        };

        _ordersSent.Add(1, tags);
        _orderDuration.Record(elapsed.TotalMilliseconds, tags);
    }

    /// <param name="status">The HTTP status, or null when no response arrived</param>
    public void ReceiptSent(string receiver, int? status, int pallets, TimeSpan elapsed)
    {
        var tags = new TagList
        {
            { "receiver", receiver },
            { "status", status?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none" }
        };

        _receiptsSent.Add(1, tags);
        _palletsSent.Add(pallets, tags);
        _receiptDuration.Record(elapsed.TotalMilliseconds, tags);
    }
}
