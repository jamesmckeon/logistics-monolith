using Throughline.Common.Models;

namespace Throughline.Modules.Ordering.Domain.Orders;

internal sealed class Order
{
    /// <param name="createdOn">When Throughline accepted the order</param>
    public Order(
        OrderId id,
        OwnerReferenceNumber ownerReferenceNumber,
        OrderContent content,
        NonFutureDateTime createdOn)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(ownerReferenceNumber);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(createdOn);

        Id = id;
        OwnerReferenceNumber = ownerReferenceNumber;
        Content = content;
        CreatedOn = createdOn;
    }

    public OrderId Id { get; }
    public OwnerReferenceNumber OwnerReferenceNumber { get; }
    public OrderContent Content { get; }

    /// <summary>
    ///     When Throughline accepted the order. Not part of <see cref="Content" />, so an identical
    ///     resend still matches the original order.
    /// </summary>
    public NonFutureDateTime CreatedOn { get; }
}