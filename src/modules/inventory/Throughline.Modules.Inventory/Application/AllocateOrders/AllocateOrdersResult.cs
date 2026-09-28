using Throughline.Modules.Inventory.Application.Models;

namespace Throughline.Modules.Inventory.Application.AllocateOrders;

internal sealed record AllocateOrdersResult
{
    private static readonly string[] BadRequestCodes =
        [AllocationError.OrderIdsEmptyCode, AllocationError.DuplicateOrderIdsCode];

    internal AllocateOrdersResult(IEnumerable<AllocatedOrder> orders, IEnumerable<AllocationError> errors)
    {
        var errorsList = errors.ToList();

        Orders = orders.ToList().AsReadOnly();
        Errors = errorsList.AsReadOnly();
    }

    internal AllocateOrdersResult(IEnumerable<AllocationError> errors)
    {
        Errors = errors.ToList().AsReadOnly();
        Orders = [];
    }

    public bool IsBadRequest => Errors.Any(a => BadRequestCodes.Contains(a.Code));

    public IReadOnlyCollection<AllocationError> Errors { get; }

    public IReadOnlyCollection<AllocatedOrder> Orders { get; }

    public static implicit operator AllocateOrdersResult(AllocationError error)
    {
        return new AllocateOrdersResult([error]);
    }
}