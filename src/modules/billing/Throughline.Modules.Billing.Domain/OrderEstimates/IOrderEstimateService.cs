namespace Throughline.Modules.Billing.Domain.OrderEstimates;

/// <summary>
///     Prices a submitted order into an estimated fulfillment quote, before any warehouse work begins.
///     The quote equals <c>Σ(line pick fee + weight handling) + destination-zone surcharge</c>, denominated
///     in USD, using the rate/zone table in force when the estimate is made.
/// </summary>
public interface IOrderEstimateService
{
    /// <summary>
    ///     Computes the fulfillment quote for <paramref name="request" />.
    /// </summary>
    /// <remarks>
    ///     The whole request is priced or rejected as a unit — invalid lines are never dropped or corrected.
    ///     The result is a failure when the request has no lines, or when any line has a quantity below 1 (each).
    ///     Pricing is exact (no rounding drift) and deterministic.  Quantities for the same sku code are summed.
    ///     The destination zone is derived from the request's address via the current rate/zone table.
    /// </remarks>
    OrderEstimate GetEstimate(OrderEstimateRequest request);
}