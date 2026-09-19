namespace Throughline.Modules.Inventory.Application.Models;

internal sealed record OrderLineModel(string SkuCode, int QuantityRequested);