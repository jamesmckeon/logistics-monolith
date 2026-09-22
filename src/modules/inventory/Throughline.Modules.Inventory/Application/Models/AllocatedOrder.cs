namespace Throughline.Modules.Inventory.Application.Models;

public sealed record AllocatedOrder(
    Guid OrderId,
    string Status,
    IReadOnlyCollection<AllocationError> Errors,
    IReadOnlyCollection<AllocationShortage> Shortages)
{
    internal const string FailedStatus = "failed";

    public static AllocatedOrder Failed(Guid orderId, string status)
    {
        return new AllocatedOrder(orderId, FailedStatus, [AllocationError.OrderNotFound], []);
    }
}