using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;

namespace Throughline.Modules.Ordering.Application.Orders;

public interface IOrderEstimateRequestBuilder
{
    Task<Result<OrderEstimateRequest>> CreateRequestAsync(CreateOrderCommand command);
}