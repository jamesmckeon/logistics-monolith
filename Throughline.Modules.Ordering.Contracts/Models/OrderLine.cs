namespace Throughline.Modules.Ordering.Contracts.Models;

public sealed class OrderLine
{
    public OrderLine(string skuCode, int quantityRequested)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skuCode);

        SkuCode = skuCode;
        QuantityRequested = quantityRequested;
    }

    public string SkuCode { get; }
    public int QuantityRequested { get; }
}