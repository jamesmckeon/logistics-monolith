namespace Throughline.Modules.Ordering.Domain.Orders;

public interface IOrdersRepository
{
    Task SaveOrderAsync(Order order);
    Task<bool> OrderExistsFor(int merchantId, string referenceNumber);
}