namespace Throughline.Modules.Inventory.Application.Models;

public sealed record AllocationError(string Code, string Description)
{
    public static AllocationError OrderNotFound(Guid orderId)
    {
        return new AllocationError("ORDER_NOT_FOUND", $"An order for id {orderId} was not found");
    }

    public static AllocationError OrderAllocating(Guid orderId)
    {
        return new AllocationError("ORDER_ALLOCATING", $"Order id {orderId} is currently being allocated");
    }
}