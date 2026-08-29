namespace Throughline.Modules.Ordering.Domain;

public interface IOrdersRepository
{
    Task SaveOrderAsync(
        Order order, CancellationToken cancellationToken = default);

    Task<bool> OrderExistsFor(
        int merchantId, string referenceNumber, CancellationToken cancellationToken = default);
}