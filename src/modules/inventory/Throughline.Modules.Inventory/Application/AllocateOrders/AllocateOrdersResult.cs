using Throughline.Modules.Inventory.Application.Models;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

public sealed record AllocateOrdersResult
{
    internal AllocateOrdersResult(IEnumerable<AllocatedOrder> orders)
    {
        Success = true;
        Orders = orders.ToList().AsReadOnly();
        Errors = [];
    }

    internal AllocateOrdersResult(IEnumerable<AllocationError> errors)
    {
        Errors = errors.ToList().AsReadOnly();
        Success = false;
        Orders = [];
    }

    public bool Success { get; }
    public IReadOnlyCollection<AllocationError> Errors { get; }

    public IReadOnlyCollection<AllocatedOrder> Orders { get; }
}