using Throughline.Modules.Inventory.Application.Models;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal sealed record AllocateOrdersResult
{
    internal AllocateOrdersResult(IEnumerable<AllocatedOrder> orders)
    {
        Success = true;
        Orders = orders.ToList().AsReadOnly();
        Errors = [];
    }

    internal AllocateOrdersResult(IEnumerable<AllocatedOrder> orders, IEnumerable<AllocationError> errors)
    {
        var errorsList = errors.ToList();

        Success = errorsList.Count == 0;
        Orders = orders.ToList().AsReadOnly();
        Errors = errorsList.AsReadOnly();
    }

    internal AllocateOrdersResult(IEnumerable<AllocationError> errors)
    {
        Errors = errors.ToList().AsReadOnly();
        Success = false;
        Orders = [];
    }

    public bool IsBadRequest => Errors.Any(a => a.Code == AllocationError.InvalidRequestCode);

    public bool Success { get; }
    public IReadOnlyCollection<AllocationError> Errors { get; }

    public IReadOnlyCollection<AllocatedOrder> Orders { get; }
}