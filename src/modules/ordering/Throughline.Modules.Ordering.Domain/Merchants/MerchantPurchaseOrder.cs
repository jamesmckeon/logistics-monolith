namespace Throughline.Modules.Ordering.Domain.Merchants;

public sealed class MerchantPurchaseOrder : ValueObject
{
    private MerchantPurchaseOrder(Merchant merchant, string purchaseOrderNumber)
    {
        Merchant = merchant;
        PurchaseOrderNumber = purchaseOrderNumber;
    }

    public Merchant Merchant { get; }
    public string PurchaseOrderNumber { get; }


    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Merchant;
        yield return PurchaseOrderNumber;
    }

    public Result<MerchantPurchaseOrder> Create(Merchant merchant, string purchaseOrderNumber)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderNumber))
            return Error.Validation("purchaseOrderNumber is required");

        return new MerchantPurchaseOrder(merchant, purchaseOrderNumber);
    }
}