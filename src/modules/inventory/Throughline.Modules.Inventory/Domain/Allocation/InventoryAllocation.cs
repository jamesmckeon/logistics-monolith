using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Domain.Allocation;

internal sealed class InventoryAllocation : ValueObject
{
    private InventoryAllocation(
        SkuReceipt skuReceipt, int quantityAllocated, AppDateTime allocatedOn, OrderLineAllocation orderLine)
    {
        ArgumentNullException.ThrowIfNull(skuReceipt);
        ArgumentNullException.ThrowIfNull(orderLine);

        SkuReceipt = skuReceipt;
        QuantityAllocated = quantityAllocated;
        AllocatedOn = allocatedOn;
        OrderLine = orderLine;
    }

    public SkuReceipt SkuReceipt { get; }
    public int QuantityAllocated { get; }
    public AppDateTime AllocatedOn { get; }
    public OrderLineAllocation OrderLine { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return SkuReceipt;
        yield return QuantityAllocated;
        yield return AllocatedOn;
    }

    public static Result<InventoryAllocation> Create()
    {
        throw new NotImplementedException();
    }
}