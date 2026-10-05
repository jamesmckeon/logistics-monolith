using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed record ReceiveDeliveryCommand(
    Guid ReceiptId,
    Guid OperatorId,
    string DeliveryReference,
    string BillOfLading,
    string CarrierScac,
    string TrailerNumber,
    string ContainerNumber,
    string ShipperName,
    IEnumerable<SubmittedPallet> Pallets)
{
    /// <summary>
    ///     Only performs validation that results in rejection of whole request; more granular/nuanced validation
    ///     is performed downstream
    /// </summary>
    /// <returns></returns>
    public Result Validate()
    {
        var errors = new List<FieldError>();

        void AddIfBlank(string value, string fieldName)
        {
            if (value.Trim().Length == 0)
            {
                errors.Add(new($"{fieldName} is required", fieldName));
            }
        }

        AddIfBlank(DeliveryReference, nameof(DeliveryReference));

        if (string.IsNullOrWhiteSpace(TrailerNumber) && string.IsNullOrWhiteSpace(ContainerNumber))
        {
            errors.Add(new("Either ContainerNumber or TrailerNumber is required"));
        }

        var scacResult = ScacCode.Validate(CarrierScac);

        if (!scacResult.Succeeded)
        {
            errors.AddRange(scacResult.Errors.Select(s =>
                new FieldError(s.Description, nameof(CarrierScac))));
        }

        var pallets = Pallets.ToArray();

        if (!pallets.Any())
        {
            errors.Add(new FieldError("Items must contain at least one item", nameof(Pallets)));
        }
        else
        {
            var duplicateLpns = Pallets.GroupBy(grp => grp.Lpn.Trim().ToUpperInvariant())
                .Select(grp => new { Lpn = grp.Key, Count = grp.Count() })
                .Where(grp => grp.Count > 1)
                .ToList();

            if (duplicateLpns.Any())
            {
                errors.Add(new("Pallets must contain unique LPNs", nameof(Pallets)));
            }

            if (pallets.Any(a => string.IsNullOrWhiteSpace(a.Sku)))
            {
                errors.Add(new("Each pallet must have a non-blank sku",
                    nameof(Pallets)));
            }

            if (pallets.Any(a => string.IsNullOrWhiteSpace(a.Lpn)))
            {
                errors.Add(new("Each pallet must have a non-blank lpn",
                    nameof(Pallets)));
            }

            if (pallets.Any(a => string.IsNullOrWhiteSpace(a.LocationId)))
            {
                errors.Add(new("Each pallet must have a non-blank location ID",
                    nameof(Pallets)));
            }

            if (pallets.Any(a => a.Quantity <= 0))
            {
                errors.Add(new("Each pallet must have quantity greater than zero",
                    nameof(Pallets)));
            }
        }

        return errors.Any() ? Result.Validation(errors) : Result.Success();
    }
}