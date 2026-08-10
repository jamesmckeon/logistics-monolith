namespace Throughline.Modules.Ordering.Application.Orders.Models;

public sealed class CreateOrderCommandItem
{
    internal CreateOrderCommandItem(string sku, int quantity)
    {
        Sku = sku;
        Quantity = quantity;
    }

    public string Sku { get; }
    public int Quantity { get; }
}