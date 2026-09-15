using Throughline.Modules.Ordering.Contracts.Models;

namespace Throughline.Modules.Ordering.Contracts.Events;

public sealed class OrderConfirmedEvent
{
    public OrderConfirmedEvent(
        Guid orderId,
        int ownerId,
        string ownerReferenceNumber,
        DateTimeOffset confirmedOn,
        IEnumerable<OrderLine> orderLines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerReferenceNumber);

        ArgumentNullException.ThrowIfNull(orderLines);

        var lines = orderLines.ToList();

        if (!lines.Any())
            throw new ArgumentException("orderLines cannot be empty", nameof(orderLines));

        OrderId = orderId;
        OwnerId = ownerId;
        ConfirmedOn = confirmedOn;
        OwnerReferenceNumber = ownerReferenceNumber;
        OrderLines = lines.AsReadOnly();
    }

    public Guid OrderId { get; }
    public int OwnerId { get; }
    public DateTimeOffset ConfirmedOn { get; }
    public string OwnerReferenceNumber { get; }
    public IReadOnlyCollection<OrderLine> OrderLines { get; }
}