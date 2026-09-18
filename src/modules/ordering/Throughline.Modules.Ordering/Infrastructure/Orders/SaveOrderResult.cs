using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

/// <summary>
///     The result of a "save new order" operation
/// </summary>
/// <param name="OrderId">The id of the order</param>
/// <param name="Created">
///     True if a new order was saved to the db; otherwise, indicates that
///     an existing order was found for <paramref name="OrderId" />, most likely as a result of a race
///     condition
/// </param>
internal sealed record SaveOrderResult(OrderId OrderId, bool Created)
{
}