using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Domain.Inventory;

internal sealed class PalletService : IPalletService
{
    public PalletServiceResult BuildPallets(PalletServiceRequest request)
    {
        var invalidPallets = GetInvalidPallets(request);
        var validPallets = request.Pallets.Where(w =>
            invalidPallets.All(all => all.LicensePlateNumber != new UpperCaseString(w.Lpn)));

        var pallets = new List<Pallet>();
        foreach (var item in validPallets)
        {
            var sku = request.OwnerSkus.Single(s => s.SkuCode == new UpperCaseString(item.Sku));
            var holdReason = string.IsNullOrWhiteSpace(item.HoldReasonCode)
                ? null
                : request.HoldReasons.Single(hr => hr.ReasonCode == new UpperCaseString(item.HoldReasonCode));
            var location = holdReason is null
                ? request.DefaultLocations.AvailableLocation
                : request.DefaultLocations.HoldLocation;
            var expiresOn = item.Expires is null ? null : AppDateTime.AssumeUtc(item.Expires.Value);
            var pallet = new Pallet(UniqueId.Create(), new(item.Lpn), sku, item.Quantity, location,
                holdReason, expiresOn, item.LotNumber);

            pallets.Add(pallet);
        }

        return new(pallets, invalidPallets);
    }

    private IReadOnlyCollection<InvalidPallet> GetInvalidPallets(
        PalletServiceRequest request)
    {
        var invalidPallets = new List<InvalidPallet>();

        foreach (var pallet in request.Pallets)
        {
            var exceptions = new List<ReceivingExceptions>();
            var sku = request.OwnerSkus.FirstOrDefault(a => a.SkuCode == new UpperCaseString(pallet.Sku));

            if (sku is null)
            {
                exceptions.Add(ReceivingExceptions.InvalidSkuCode);
            }
            else
            {
                if (sku.IsLotTracked && string.IsNullOrWhiteSpace(pallet.LotNumber))
                {
                    exceptions.Add(ReceivingExceptions.LotNumberRequired);
                }

                if (sku.IsExpirationTracked && pallet.Expires == null)
                {
                    exceptions.Add(ReceivingExceptions.ExpirationDateRequired);
                }
            }

            if (request.ExistingLpns.Any(a => a == new UpperCaseString(pallet.Lpn)))
            {
                exceptions.Add(ReceivingExceptions.DuplicateLpn);
            }

            if (request.Locations.All(all => all.Id != new UpperCaseString(pallet.LocationId)))
            {
                exceptions.Add(ReceivingExceptions.InvalidLocation);
            }

            if (!string.IsNullOrWhiteSpace(pallet.HoldReasonCode))
            {
                var holdReason =
                    request.HoldReasons.FirstOrDefault(a => a.ReasonCode == new UpperCaseString(pallet.HoldReasonCode));

                if (holdReason is null || !holdReason.IsActive)
                {
                    exceptions.Add(ReceivingExceptions.InvalidHoldReason);
                }
            }

            if (exceptions.Any())
            {
                var exceptedPallet = new InvalidPallet(
                    UniqueId.Create(),
                    request.OwnerId,
                    new(pallet.Sku),
                    new(pallet.Lpn),
                    pallet.Quantity,
                    request.DefaultLocations.ExceptionLocation,
                    pallet.LocationId,
                    pallet.LotNumber,
                    pallet.Expires.HasValue ? AppDateTime.AssumeUtc(pallet.Expires.Value) : null,
                    exceptions.ToArray());

                invalidPallets.Add(exceptedPallet);
            }
        }

        return invalidPallets;
    }
}