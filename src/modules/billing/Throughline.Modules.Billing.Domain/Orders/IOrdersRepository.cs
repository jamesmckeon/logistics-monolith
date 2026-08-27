namespace Throughline.Modules.Billing.Domain.Orders;

public interface IOrdersRepository
{
    Task SaveOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<bool> OrderExistsFor(int merchantId, string referenceNumber, CancellationToken cancellationToken = default);

    Task<Order?> GetOrderByMerchantReferenceAsync(
        int merchantId, string referenceNumber,
        CancellationToken cancellationToken = default);
}