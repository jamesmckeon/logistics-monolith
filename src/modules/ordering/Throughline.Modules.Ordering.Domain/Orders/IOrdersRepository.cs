namespace Throughline.Modules.Ordering.Domain.Orders;

public interface IOrdersRepository
{
    Task SaveOrderAsync(Order order);
}