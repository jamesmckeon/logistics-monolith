using Throughline.Common.Results;

namespace Throughline.Modules.Ordering.Application;

public sealed record CreateOrderCommand(
    int MerchantId,
    string PurchaseOrderNumber,
    string ReferenceNumber,
    string StreetAddressOne,
    string? StreetAddressTwo,
    string City,
    string State,
    string PostalCode,
    IEnumerable<(string Sku, int Quantity)> Items)
{
    public Result Validate()
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(PurchaseOrderNumber))
            AddRequiredError("PurchaseOrderNumber", errors);

        if (string.IsNullOrWhiteSpace(ReferenceNumber))
            AddRequiredError("ReferenceNumber", errors);

        if (string.IsNullOrWhiteSpace(StreetAddressOne))
            AddRequiredError("StreetAddressOne", errors);

        if (string.IsNullOrWhiteSpace(City))
            AddRequiredError("City", errors);

        if (string.IsNullOrWhiteSpace(State))
            AddRequiredError("State", errors);

        if (string.IsNullOrWhiteSpace(PostalCode))
            AddRequiredError("PostalCode", errors);

        if (Items is null || !Items.Any())
            AddRequiredError("Items", errors);

        return errors.Any() ? Result.Validation(errors) : Result.Success();
    }

    private static void AddRequiredError(string paramName, List<Error> errors)
    {
        errors.Add(Error.IsRequired(paramName));
    }
}