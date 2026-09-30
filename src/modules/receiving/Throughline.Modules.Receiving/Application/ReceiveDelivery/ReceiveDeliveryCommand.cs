using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.Shipments;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed record ReceiveDeliveryCommand(
    int OwnerId,
    Guid OperatorId,
    string DeliveryReference,
    string BillOfLading,
    string CarrierScac,
    string TrailerNumber,
    string ContainerNumber,
    string ShipperName,
    IEnumerable<ReceiveDeliveryCommandItem> Items)
{
    public Result ValidateCommand(ReceiveDeliveryCommand command)
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
        AddIfBlank(BillOfLading, nameof(BillOfLading));
        AddIfBlank(TrailerNumber, nameof(TrailerNumber));
        AddIfBlank(ContainerNumber, nameof(ContainerNumber));
        AddIfBlank(ShipperName, nameof(ShipperName));

        var scacResult = ScacCode.Create(CarrierScac);

        if (!scacResult.Succeeded)
        {
            errors.AddRange(scacResult.Errors.Select(s =>
                new FieldError(s.Description, nameof(CarrierScac))));
        }

        var pallets = Items.ToArray();

        if (!pallets.Any())
        {
            errors.Add(new FieldError("Items must contain at least one item", nameof(Items)));
        }
        else
        {
            throw new NotImplementedException("validate pallets");
        }

        return errors.Any() ? Result.Validation(errors) : Result.Success();
    }
}