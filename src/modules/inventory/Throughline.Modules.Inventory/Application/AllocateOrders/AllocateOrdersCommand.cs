namespace Throughline.Modules.Inventory.Application.AllocateOrders;

public sealed record AllocateOrdersCommand(IEnumerable<Guid> OrderIds)
{
}