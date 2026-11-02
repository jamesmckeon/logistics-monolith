namespace Throughline.Modules.Inventory.Application.Models;

internal sealed record AllocationError(string Code, string Description)
{
    public const string OrderIdsEmptyCode = "ORDERIDS_MISSING";
    public const string DuplicateOrderIdsCode = "DUPLICATE_ORDERS";
    public const string OrderAllocatingCode = "ORDER_ALLOCATING";
    public const string PolicyNotSatisfiedCode = "POLICY_NOT_SATISFIED";
    public const string OrderCapExceededCode = "MAX_ORDERS_EXCEEDED";

    public static AllocationError OrderNotFound(Guid orderId) =>
        new("ORDER_NOT_FOUND", $"An order for id {orderId} was not found");

    public static AllocationError OrderAllocating(Guid orderId) =>
        new(OrderAllocatingCode,
            $"Order id {orderId} is currently being allocated");

    public static AllocationError OrderIdsEmpty(string message) => new(OrderIdsEmptyCode, message);

    public static AllocationError OrderCapExceeded() => new(OrderCapExceededCode,
        "Only 50 order ids can be allocated at once");

    public static AllocationError PolicyNotSatisfied(string message) => new(PolicyNotSatisfiedCode, message);

    public static AllocationError DuplicateOrderIds(IEnumerable<Guid> orderIds)
    {
        return new AllocationError(DuplicateOrderIdsCode,
            "The request contains the following duplicate order ids: " +
            $"{string.Join(", ", orderIds.Select(o => o.ToString()))}");
    }
}