namespace Throughline.Simulator.Ledger;

/// <summary>
///     The simulator's own records: what customers sold, what they bought, and which trailers are on
///     the way. Kept in the simulator's database so a restart loses nothing.
/// </summary>
internal interface ILedger
{
    /// <summary>
    ///     Records an accepted order's lines; recording the same order again changes nothing
    /// </summary>
    Task RecordSaleAsync(
        int ownerId, string referenceNumber, IReadOnlyList<SoldLine> lines, DateTimeOffset soldAt,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Units sold per SKU from <paramref name="from" /> (inclusive) to <paramref name="to" /> (exclusive)
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetUnitsSoldAsync(
        int ownerId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    /// <summary>
    ///     When the owner's latest purchase order was placed, or null if it has never ordered
    /// </summary>
    Task<DateTimeOffset?> GetLastPurchaseOrderAtAsync(int ownerId, CancellationToken cancellationToken);

    Task SavePurchaseOrderAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken);

    /// <summary>
    ///     Claims the trailer that's been waiting longest, or returns null if none is due. A claim
    ///     that isn't completed within 30 minutes (the receiver crashed) can be claimed again.
    /// </summary>
    Task<InboundShipment?> ClaimDueShipmentAsync(
        string claimant, DateTimeOffset now, CancellationToken cancellationToken);

    Task MarkReceivedAsync(Guid shipmentId, int statusCode, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>
    ///     Throughline rejected the receipt; it won't be tried again
    /// </summary>
    Task MarkRejectedAsync(Guid shipmentId, int statusCode, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>
    ///     Gives a claimed trailer back, so it's tried again later
    /// </summary>
    Task ReleaseAsync(Guid shipmentId, CancellationToken cancellationToken);
}
