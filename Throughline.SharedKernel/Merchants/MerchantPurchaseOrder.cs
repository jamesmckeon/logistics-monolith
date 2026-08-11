namespace Throughline.SharedKernel.Merchants;

public sealed class MerchantPurchaseOrder : ValueObject
{
    public MerchantPurchaseOrder(Merchant merchant, string purchaseOrderNumber)
    {
        ArgumentNullException.ThrowIfNull(merchant);
        ArgumentException.ThrowIfNullOrWhiteSpace(purchaseOrderNumber);

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
}