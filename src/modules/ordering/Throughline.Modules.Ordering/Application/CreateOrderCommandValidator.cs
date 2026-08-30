using Throughline.Common.Results;
using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Ordering.Application;

public sealed class CreateOrderCommandValidator
{
    public Result ValidateCommand(CreateOrderCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(command.PurchaseOrderNumber))
            AddRequiredError("PurchaseOrderNumber", errors);

        if (string.IsNullOrWhiteSpace(command.ReferenceNumber))
            AddRequiredError("ReferenceNumber", errors);

        if (string.IsNullOrWhiteSpace(command.StreetAddressOne))
            AddRequiredError("StreetAddressOne", errors);

        if (string.IsNullOrWhiteSpace(command.City))
            AddRequiredError("City", errors);

        if (string.IsNullOrWhiteSpace(command.State))
            AddRequiredError("State", errors);
        else if (command.State.Trim().Length != 2 || command.State.Trim().Any(a => !char.IsLetter(a)))
            errors.Add(new Error("state must be 2 alpha characters", "state"));

        if (string.IsNullOrWhiteSpace(command.PostalCode))
        {
            AddRequiredError("PostalCode", errors);
        }
        else
        {
            var postalCodeResult = PostalCode.Create(command.PostalCode);
            
            if (!postalCodeResult.Succeeded)
                errors.AddRange(postalCodeResult.Errors);
        }

        if (command.Items is null)
        {
            AddRequiredError("Items", errors);
            return Result.Validation(errors);
        }

        var itemsArray = command.Items.ToArray();

        if (!itemsArray.Any())
        {
            errors.Add(new Error("items must contain at least one item."));
            return Result.Validation(errors);
        }

        if (itemsArray.Any(x => string.IsNullOrWhiteSpace(x.Sku)))
            errors.Add(new Error("each item must have a sku."));

        if (itemsArray.Any(x => x.Quantity <= 0))
            errors.Add(new Error("each item must have a quantity greater than 0."));

        var duplicateskus = itemsArray
            .GroupBy(x => x.Sku.Trim().ToUpperInvariant())
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();

        if (duplicateskus.Any())
            errors.Add(new Error($"Items contains duplicate skus: {string.Join(", ", duplicateskus)}"));

        if (errors.Any())
            return Result.Validation(errors);

        return Result.Success();
    }

    private static void AddRequiredError(string paramName, List<Error> errors)
    {
        errors.Add(Error.IsRequired(paramName));
    }
}