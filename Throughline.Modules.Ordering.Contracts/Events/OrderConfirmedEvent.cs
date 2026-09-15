using Throughline.Common.Events;
using Throughline.Modules.Ordering.Contracts.Models;

namespace Throughline.Modules.Ordering.Contracts.Events;

public sealed record OrderConfirmedEvent : IntegrationEventBase
{
    public OrderConfirmedEvent(
        Guid eventId,
        DateTimeOffset occurredOnUtc,
        Guid orderId,
        int ownerId,
        string ownerReferenceNumber,
        IEnumerable<OrderLine> orderLines) : base(eventId, occurredOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerReferenceNumber);

        ArgumentNullException.ThrowIfNull(orderLines);

        var lines = orderLines.ToList();

        if (!lines.Any())
            throw new ArgumentException("orderLines cannot be empty", nameof(orderLines));

        OrderId = orderId;
        OwnerId = ownerId;
        OwnerReferenceNumber = ownerReferenceNumber;
        OrderLines = lines.AsReadOnly();
    }

    public Guid OrderId { get; }
    public int OwnerId { get; }
    public string OwnerReferenceNumber { get; }
    public IReadOnlyCollection<OrderLine> OrderLines { get; }
}