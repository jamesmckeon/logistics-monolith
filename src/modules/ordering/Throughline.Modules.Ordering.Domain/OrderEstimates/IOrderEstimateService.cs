namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public interface IOrderEstimateService
{
    Result<OrderEstimate> GetEstimate(OrderEstimateRequest request);
}