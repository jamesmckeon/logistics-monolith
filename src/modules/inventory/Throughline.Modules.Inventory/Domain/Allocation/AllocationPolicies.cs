namespace Throughline.Modules.Inventory.Domain.Allocation;

/// <summary>
///     Identifies owner allocation policies
/// </summary>
internal enum AllocationPolicies
{
    /// <summary>
    ///     All lines on an order must be fully allocatable from current
    ///     inventory for the order to be allocated
    /// </summary>
    ShipComplete,

    /// <summary>
    ///     Allows for lines to be partially allocated as a result of an allocation
    ///     process
    /// </summary>
    Partial
}