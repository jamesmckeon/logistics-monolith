namespace Throughline.Modules.Ordering.Domain.Orders;

internal sealed class Order
{
    internal Order(
        OrderId id,
        OwnerReferenceNumber ownerReferenceNumber,
        OrderContent content)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(ownerReferenceNumber);
        ArgumentNullException.ThrowIfNull(content);

        Id = id;
        OwnerReferenceNumber = ownerReferenceNumber;
        Content = content;
    }

    public OrderId Id { get; }
    public OwnerReferenceNumber OwnerReferenceNumber { get; }
    public OrderContent Content { get; }
}