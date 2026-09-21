using Throughline.Modules.Inventory.Domain.Allocation;

namespace Throughline.Modules.Inventory.Application.Models;

internal sealed class OrderAllocationModel
{
    public OrderAllocationModel(
        int ownerId,
        Guid orderId,
        string allocationStatus,
        IReadOnlyCollection<OrderLineModel> lines)
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
    public IReadOnlyCollection<OrderLineModel> Lines { get; }

    public static OrderAllocationModel FromOrder(OrderAllocation order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new(
            order.OwnerId,
            order.Id,
            order.AllocationStatus.ToString(),
            order.OrderLines.Select(ol => new OrderLineModel(
                ol.SkuCode, ol.QuantityRequested)).ToList().AsReadOnly());
    }
}