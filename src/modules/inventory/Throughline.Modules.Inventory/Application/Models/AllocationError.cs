namespace Throughline.Modules.Inventory.Application.Models;

public sealed record AllocationError(string Code, string Description)
{
    public static AllocationError OrderNotFound => new("ORDER_NOT_FOUND", "Order not found for this owner");
}