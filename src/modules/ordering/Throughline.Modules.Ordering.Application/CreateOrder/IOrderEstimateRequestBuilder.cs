using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;

namespace Throughline.Modules.Ordering.Application.CreateOrder;

/// <summary>
///     Assembles a priceable <see cref="OrderEstimateRequest" /> from a validated
///     <see cref="CreateOrderCommand" /> by resolving the merchant's pricing reference data.
/// </summary>
public interface IOrderEstimateRequestBuilder
{
    /// <summary>
    ///     Builds the estimate request for <paramref name="command" />. Consolidates the order items
    ///     by SKU — quantities of duplicate SKUs are summed into a single line — then pairs each
    ///     distinct SKU + total quantity with its unit weight (SKU attributes) and unit pick fee
    ///     (pick-fee table). Also resolves the one zone surcharge whose postal zone contains the
    ///     destination postal code, and the merchant's handling rate (merchant-rate query).
    /// </summary>
    /// <returns>
    ///     Success: a fully populated request — one item per <em>distinct</em> SKU, plus the zone
    ///     surcharge and handling rate. Fails fast (no request built) on the first reference datum
    ///     that does not cover the submission: a submitted SKU with no pick fee or no attributes, a
    ///     destination postal code outside every configured zone, or a merchant with no handling rate.
    ///     Throughline guarantees this reference data is complete and current, so any such gap is
    ///     returned as <see cref="ErrorType.Unavailable" />.
    /// </returns>
    Task<Result<OrderEstimateRequest>> CreateRequestAsync(
        CreateOrderCommand command, CancellationToken cancellationToken = default);
}