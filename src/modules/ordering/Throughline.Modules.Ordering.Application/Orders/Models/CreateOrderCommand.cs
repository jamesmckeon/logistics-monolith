using Throughline.Modules.Ordering.Domain.Models;

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
        DestinationRequest destination,
        IEnumerable<CreateOrderCommandItem> orderItems)
    {
        MerchantId = merchantId;
        PurchaseOrderNumber = purchaseOrderNumber;
        Destination = destination;
        OrderItems = orderItems;
        ReferenceNumber = referenceNumber;
    }

    public int MerchantId { get; }
    public string PurchaseOrderNumber { get; }
    public DestinationRequest Destination { get; }
    public IEnumerable<CreateOrderCommandItem> OrderItems { get; }
    public string ReferenceNumber { get; }

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
        string streetAddressTwo,
        string locality,
        string region,
        string postalCode,
        string countryCode,
        IEnumerable<(string Sku, int Quantity)> items,
        string referenceNumber)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(purchaseOrderNumber))
            AddError("purchaseOrderNumber is required", errors);

        if (string.IsNullOrWhiteSpace(referenceNumber))
            AddError("referenceNumber is required", errors);

        if (string.IsNullOrWhiteSpace(locality))
            AddError("locality is required", errors);

        if (string.IsNullOrWhiteSpace(region))
            AddError("region is required", errors);

        if (string.IsNullOrWhiteSpace(postalCode))
            AddError("postalCode is required", errors);
        else if (!PostalCode.IsValid(postalCode))
            AddError("postalCode is not a valid postal code", errors);

        if (string.IsNullOrWhiteSpace(countryCode))
            AddError("countryCode is required", errors);

        if (items is null)
        {
            AddError("items is required", errors);
            return errors.ToArray();
        }

        var itemsArray = items.ToArray();

        if (!itemsArray.Any())
        {
            AddError("items must contain at least one item", errors);
            return errors.ToArray();
        }

        if (itemsArray.Any(x => string.IsNullOrWhiteSpace(x.Sku)))
            AddError("each item must have a sku", errors);

        if (itemsArray.Any(x => x.Quantity <= 0))
            AddError("each item must have a quantity greater than 0", errors);

        if (errors.Any())
            return errors.ToArray();

        return new CreateOrderCommand(
            merchantId,
            purchaseOrderNumber,
            referenceNumber,
            new DestinationRequest(
                streetAddressOne, streetAddressTwo, locality, region, postalCode, countryCode),
            itemsArray.Select(i => new CreateOrderCommandItem(i.Sku, i.Quantity)));
    }

    private static void AddError(string error, List<Error> errors)
    {
        errors.Add(Error.Validation(error));
    }
}