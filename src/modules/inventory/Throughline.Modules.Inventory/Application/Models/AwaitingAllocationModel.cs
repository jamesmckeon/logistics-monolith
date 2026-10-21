namespace Throughline.Modules.Inventory.Application.Models;

/// <summary>
///     An owner's allocation worklist: orders awaiting allocation, earliest accepted first. It may be
///     truncated to the requested batch size; allocate and fetch again for more.
/// </summary>
internal sealed record AwaitingAllocationModel(IReadOnlyCollection<OrderAllocationModel> Orders);
