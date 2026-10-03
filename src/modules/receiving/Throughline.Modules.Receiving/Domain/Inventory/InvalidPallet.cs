using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.Locations;

namespace Throughline.Modules.Receiving.Domain.Inventory;

/// <summary>
///     A pallet that hasn't been received into inventory due to invalid state
/// </summary>
internal sealed class InvalidPallet : Entity<UniqueId>
{
    public InvalidPallet(
        UniqueId id,
        int ownerId,
        UpperCaseString skuCode,
        UpperCaseString lpn,
        int quantity,
        ReceivingLocation location,
        string requestLocationId,
        IEnumerable<ReceivingExceptions> exceptions) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var exceptionArray = exceptions.ToArray();

        if (exceptionArray.Length == 0)
        {
            throw new ArgumentException("exceptions must contain at least one item", nameof(exceptions));
        }

        if (!location.IsExceptionLocation)
        {
            throw new ArgumentException("location must be able to store pallets with exceptions", nameof(location));
        }

        OwnerId = ownerId;
        SkuCode = skuCode;
        Quantity = quantity;
        LicensePlateNumber = lpn;
        Exceptions = exceptionArray.AsReadOnly();
        Location = location;
        RequestLocationId = requestLocationId;
    }

    public int OwnerId { get; }
    public UpperCaseString SkuCode { get; }
    public UpperCaseString LicensePlateNumber { get; }
    public int Quantity { get; }
    public IReadOnlyCollection<ReceivingExceptions> Exceptions { get; }

    /// <summary>
    ///     The location the excepted pallet was received into; not necessarily the same as
    ///     the location id that was provided with the pallet in the original request
    /// </summary>
    public ReceivingLocation Location { get; }

    /// <summary>
    ///     The location id provided with the LPN in the original request
    /// </summary>
    public string RequestLocationId { get; }
}