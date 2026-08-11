namespace Throughline.Modules.Ordering.Application.Orders.Models;

public sealed class CreateOrderCommand
{
    private CreateOrderCommand(
        int merchantId,
        string purchaseOrderNumber,
        DestinationRequest destination,
        IEnumerable<CreateOrderCommandItem> orderItems)
    {
        MerchantId = merchantId;
        PurchaseOrderNumber = purchaseOrderNumber;
        Destination = destination;
        OrderItems = orderItems;
    }

    public int MerchantId { get; }
    public string PurchaseOrderNumber { get; }
    public DestinationRequest Destination { get; }
    public IEnumerable<CreateOrderCommandItem> OrderItems { get; }
    public string ReferenceNumber { get; }

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

        if (string.IsNullOrWhiteSpace(countryCode))
            AddError("countryCode is required", errors);

        if (items is null || !items.Any())
            AddError("items is required", errors);

        if (items != null)
        {
            if (items.Any(x => string.IsNullOrWhiteSpace(x.Sku)))
                AddError("each item must have a sku", errors);

            if (items.Any(x => x.Quantity <= 0))
                AddError("each item must have a quantity greater than 0", errors);
        }

        if (errors.Any())
            return errors.ToArray();

        return new CreateOrderCommand(
            merchantId,
            purchaseOrderNumber,
            new DestinationRequest(
                streetAddressOne, streetAddressTwo, locality, region, postalCode, countryCode),
            items.Select(i => new CreateOrderCommandItem(i.Sku, i.Quantity))
        );
    }

    private static void AddError(string error, List<Error> errors)
    {
        errors.Add(Error.Validation(error));
    }
}