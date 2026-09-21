namespace Throughline.Modules.Inventory.Application.Models;

internal sealed class OrderAllocationModel
{
    public OrderAllocationModel(
        int ownerId,
        Guid orderId,
        string allocationStatus,
        IReadOnlyCollection<OrderLineAllocationModel> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(allocationStatus);

        if (!lines.Any())
            throw new ArgumentException("lines must contain at least one item", nameof(lines));

        OwnerId = ownerId;
        OrderId = orderId;
        AllocationStatus = allocationStatus;
        Lines = lines;
    }

    public int OwnerId { get; }
    public Guid OrderId { get; }
    public string AllocationStatus { get; }
    public IReadOnlyCollection<OrderLineAllocationModel> Lines { get; }
}