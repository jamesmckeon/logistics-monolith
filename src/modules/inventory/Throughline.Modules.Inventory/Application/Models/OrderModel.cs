using Throughline.Modules.Inventory.Domain.Orders;

namespace Throughline.Modules.Inventory.Application.Models;

internal sealed class OrderModel
{
    public OrderModel(
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

    public static OrderModel FromOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new(
            order.Id.OwnerId,
            order.Id.OrderId,
            order.AllocationStatus.ToString(),
            order.OrderLines.Select(ol => new OrderLineModel(
                ol.SkuCode, ol.QuantityRequested)).ToList().AsReadOnly());
    }
}