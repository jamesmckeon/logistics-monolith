using Throughline.SharedKernel.Merchants;

namespace Throughline.SharedKernel.Skus;

public sealed class Sku
{
    public Merchant Merchant { get; }
    public SkuCode SkuCode { get; }
    public decimal Weight { get; }
}