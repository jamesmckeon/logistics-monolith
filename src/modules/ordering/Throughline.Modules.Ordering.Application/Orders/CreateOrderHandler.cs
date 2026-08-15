using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Application.Orders;

public sealed class CreateOrderHandler
{
    private IOrderEstimateService _estimateService;
    private IOrdersRepository _ordersRepository;
    private IOrderEstimateRequestBuilder _requestBuilder;

    public CreateOrderHandler(
        IOrderEstimateRequestBuilder requestBuilder,
        IOrderEstimateService estimateService,
        IOrdersRepository ordersRepository)
    {
        _requestBuilder = requestBuilder;
        _estimateService = estimateService;
        _ordersRepository = ordersRepository;
    }

    private Result<CreateOrderResult> CreateOrderAsync(CreateOrderCommand command)
    {
        throw new NotImplementedException();
    }
}