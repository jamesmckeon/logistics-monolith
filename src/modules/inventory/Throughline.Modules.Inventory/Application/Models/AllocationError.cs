namespace Throughline.Modules.Inventory.Application.Models;

internal sealed record AllocationError(string Code, string Description)
{
    public const string OrderIdsEmptyCode = "ORDERIDS_MISSING";
    public const string DuplicateOrderIdsCode = "DUPLICATE_ORDERS";
    public const string OrderAllocatingCode = "ORDER_ALLOCATING";
    public const string PolicyNotSatisfiedCode = "POLICY_NOT_SATISFIED";

    public static AllocationError OrderNotFound(Guid orderId)
    {
        return new AllocationError("ORDER_NOT_FOUND", $"An order for id {orderId} was not found");
    }

    public static AllocationError OrderAllocating(Guid orderId)
    {
        return new AllocationError(OrderAllocatingCode,
            $"Order id {orderId} is currently being allocated");
    }

    public static AllocationError OrderIdsEmpty(string message)
    {
        return new AllocationError(OrderIdsEmptyCode, message);
    }

    public static AllocationError PolicyNotSatisfied(string message)
    {
        return new AllocationError(PolicyNotSatisfiedCode, message);
    }


    public static AllocationError DuplicateOrderIds(IEnumerable<Guid> orderIds)
    {
        return new AllocationError(DuplicateOrderIdsCode,
            "The request contains the following duplicate order ids: " +
            $"{string.Join(", ", orderIds.Select(o => o.ToString()))}");
    }
}