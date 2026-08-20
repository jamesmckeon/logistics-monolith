using Throughline.Modules.Ordering.Application.Orders.Models;

namespace Throughline.Modules.Ordering.Application.CreateOrder;

public interface ICreateOrderHandler
{
    Task<Result<OrderModel>> CreateOrderAsync(
        CreateOrderCommand command, CancellationToken cancellationToken = default);
}