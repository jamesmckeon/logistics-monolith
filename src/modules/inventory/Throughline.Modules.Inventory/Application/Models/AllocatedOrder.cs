namespace Throughline.Modules.Inventory.Application.Models;

public sealed record AllocatedOrder(
    Guid OrderId,
    string Status,
    IReadOnlyCollection<SkuCodeShortage> Shortages,
    IEnumerable<AllocationError> Errors)
{
    private const string FullyAllocatedStatus = "fullyAllocated";
    private const string FailedStatus = "failed";
    private const string AllShortStatus = "notAllocated";
    public const string PartiallyAllocatedStatus = "partiallyAllocated";

    internal static AllocatedOrder FullyAllocated(Guid orderId)
    {
        return new(orderId, FullyAllocatedStatus, [], []);
    }

    internal static AllocatedOrder Failed(Guid orderId, AllocationError error)
    {
        return new AllocatedOrder(orderId, FailedStatus, [], [error]);
    }

    internal static AllocatedOrder AllShort(Guid orderId,
        IEnumerable<SkuCodeShortage> shortages)
    {
        return new AllocatedOrder(orderId, AllShortStatus, shortages.ToList().AsReadOnly(), []);
    }

    internal static AllocatedOrder PartiallyAllocated(Guid orderId,
        IEnumerable<SkuCodeShortage> shortages)
    {
        return new AllocatedOrder(orderId, PartiallyAllocatedStatus, shortages.ToList().AsReadOnly(), []);
    }
}