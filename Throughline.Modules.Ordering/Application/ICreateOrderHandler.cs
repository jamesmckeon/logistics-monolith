using Throughline.Common.Results;
using Throughline.Modules.Ordering.Models;

namespace Throughline.Modules.Ordering.Application;

public interface ICreateOrderHandler
{
    Task<Result<OrderModel>> CreateOrderAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default);
}