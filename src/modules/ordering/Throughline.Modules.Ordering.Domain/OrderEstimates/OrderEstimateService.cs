namespace Throughline.Modules.Ordering.Domain.OrderEstimates;

public class OrderEstimateService : IOrderEstimateService
{
    public Result<OrderEstimate> GetEstimate(OrderEstimateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // should I trust the application here and not validate request data?

        var zoneCharge = request.ZoneCharges.SingleOrDefault(s =>
            s.PostalZone.Includes(request.DestinationCode));

        /*
        if (zoneCharge == null)
            return Result<OrderEstimate>.Failed(Error.Unexpected(
                $"Unable to locate a surcharge for postal code {request.DestinationCode}"));
*/

        throw new NotImplementedException();
    }
}