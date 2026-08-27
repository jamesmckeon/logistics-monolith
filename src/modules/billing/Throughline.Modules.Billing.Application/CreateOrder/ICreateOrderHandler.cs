using Throughline.Modules.Billing.Application.Orders.Models;

namespace Throughline.Modules.Billing.Application.CreateOrder;

public interface ICreateOrderHandler
{
    Task<Result<OrderModel>> CreateOrderAsync(
        CreateOrderCommand command, CancellationToken cancellationToken = default);
}