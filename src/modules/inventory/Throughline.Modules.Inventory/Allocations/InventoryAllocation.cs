using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Receipts;

namespace Throughline.Modules.Inventory.Allocations;

internal sealed class InventoryAllocation : ValueObject
{
    public InventoryAllocation(SkuReceipt receipt, int quantityAllocated, AppDateTime allocatedOn)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(allocatedOn);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityAllocated);

        if (quantityAllocated > receipt.Quantity)
            throw new ArgumentOutOfRangeException(
                "Allocated quantity cannot exceed received quantity",
                nameof(quantityAllocated));

        Receipt = receipt;
        QuantityAllocated = quantityAllocated;
        AllocatedOn = allocatedOn;
    }

    public SkuReceipt Receipt { get; }
    public int QuantityAllocated { get; }
    public AppDateTime AllocatedOn { get; }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Receipt;
        yield return AllocatedOn;
    }

    public static Result<InventoryAllocation> Create(
        SkuReceipt receipt, int quantityAllocated, AppDateTime allocatedOn)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(allocatedOn);

        var errors = new List<Error>();

        if (quantityAllocated <= 0)
            errors.Add(new Error("quantityAllocated must be greater than zero"));

        if (quantityAllocated > receipt.Quantity)
            errors.Add(new Error("quantityAllocated can't exceed quantity received"));

        return errors.Any()
            ? Result<InventoryAllocation>.Validation(errors)
            : new InventoryAllocation(receipt, quantityAllocated, allocatedOn);
    }
}