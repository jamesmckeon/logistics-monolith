using Throughline.SharedKernel.Merchants;

namespace Throughline.Modules.Ordering.Domain.Orders;

public sealed class Order
{
    private Order(int id, MerchantPurchaseOrder merchantPurchaseOrder)
    {
        Id = id;
        MerchantPurchaseOrder = merchantPurchaseOrder;
    }

    public int Id { get; }
    public MerchantPurchaseOrder MerchantPurchaseOrder { get; }

    public Result<Order> Create(int id, MerchantPurchaseOrder merchantPurchaseOrder)
    {
        return new Order(id, merchantPurchaseOrder);
    }
}