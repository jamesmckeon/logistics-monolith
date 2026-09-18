using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Infrastructure.Orders;

internal sealed record SaveOrderResult(OrderId OrderId, bool Created)
{
}