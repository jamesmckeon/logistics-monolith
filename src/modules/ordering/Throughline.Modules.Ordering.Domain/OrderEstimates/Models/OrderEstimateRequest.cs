namespace Throughline.Modules.Ordering.Domain.OrderEstimates.Models;

public class OrderEstimateRequest
{
    public OrderEstimateRequest(
        PostalCode destinationCode,
        decimal handlingRate)
    {
        ArgumentNullException.ThrowIfNull(destinationCode);
        ArgumentOutOfRangeException.th
    }

    public PostalCode DestinationCode { get; }
    public decimal HandlingRate { get; }
}

public sealed class OrderEstimateRequestItem
{
}