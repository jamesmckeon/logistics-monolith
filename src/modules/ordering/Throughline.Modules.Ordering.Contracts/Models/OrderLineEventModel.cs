namespace Throughline.Modules.Ordering.Contracts.Models;

public sealed record OrderLineEventModel(string SkuCode, int QuantityRequested)
{
}