namespace Throughline.Modules.Ordering.Application.Orders.Models;

/// <summary>
///     A merchant's request to intake an order and receive an accepted acknowledgment with an
///     estimated fulfillment quote.
/// </summary>
public sealed class CreateOrderCommand
{
    private CreateOrderCommand(
        int merchantId,
        string purchaseOrderNumber,
        string referenceNumber,
        string streetAddressOne,
        string? streetAddressTwo,
        string city,
        string state,
        string postalCode,
        IEnumerable<(string Sku, int Quantity)> items)
    {
        MerchantId = merchantId;
        PurchaseOrderNumber = purchaseOrderNumber.Trim();
        ReferenceNumber = referenceNumber.Trim();
        StreetAddressOne = streetAddressOne.Trim();
        StreetAddressTwo = streetAddressTwo?.Trim();
        City = city.Trim();
        Items = items;
        ReferenceNumber = referenceNumber.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
    }

    public int MerchantId { get; }
    public string PurchaseOrderNumber { get; }
    public IEnumerable<(string Sku, int Quantity)> Items { get; }
    public string ReferenceNumber { get; }
    public string StreetAddressOne { get; }
    public string? StreetAddressTwo { get; }
    public string State { get; }
    public string City { get; }
    public string PostalCode { get; }


    /// <summary>
    ///     Parses raw submission input into a valid command, accumulating all structural
    ///     violations. Fails (no instance created) unless there is at least one item, every item
    ///     has a SKU and quantity > 0, the required address/reference fields are present, and the
    ///     postal code is a valid format.
    /// </summary>
    /// <returns>The command, or the collected <see cref="Error" />s on invalid input.</returns>
    public static Result<CreateOrderCommand> Create(
        int merchantId,
        string purchaseOrderNumber,
        string streetAddressOne,
        string? streetAddressTwo,
        string city,
        string state,
        string postalCode,
        IEnumerable<(string Sku, int Quantity)> items,
        string referenceNumber)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(purchaseOrderNumber))
            AddError(nameof(purchaseOrderNumber), errors);

        if (string.IsNullOrWhiteSpace(referenceNumber))
            AddError(nameof(referenceNumber), errors);

        if (string.IsNullOrWhiteSpace(city))
            AddError(nameof(city), errors);

        if (string.IsNullOrWhiteSpace(state))
            AddError(nameof(state), errors);

        if (string.IsNullOrWhiteSpace(postalCode))
            AddError(nameof(postalCode), errors);

        if (items is null)
        {
            AddError("items is required", errors);
            return Result<CreateOrderCommand>.Validation(errors);
        }

        var itemsArray = items.ToArray();

        if (!itemsArray.Any())
        {
            AddError("items must contain at least one item", errors);
            return Result<CreateOrderCommand>.Validation(errors);
        }

        if (itemsArray.Any(x => string.IsNullOrWhiteSpace(x.Sku)))
            AddError("each item must have a sku", errors);

        if (itemsArray.Any(x => x.Quantity <= 0))
            AddError("each item must have a quantity greater than 0", errors);

        if (errors.Any())
            return Result<CreateOrderCommand>.Validation(errors);

        return new CreateOrderCommand(
            merchantId,
            purchaseOrderNumber,
            referenceNumber,
            streetAddressOne,
            streetAddressTwo,
            city,
            state,
            postalCode,
            itemsArray);
    }

    private static void AddError(string paramName, List<Error> errors)
    {
        errors.Add(Error.IsRequired(paramName));
    }
}