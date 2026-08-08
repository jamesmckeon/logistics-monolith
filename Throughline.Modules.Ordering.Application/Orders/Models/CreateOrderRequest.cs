namespace Throughline.Modules.Ordering.Application.Orders.Models;

public sealed class CreateOrderRequest
{
    private CreateOrderRequest(
        int merchantId,
        string purchaseOrderNumber,
        DestinationRequest destination,
        IEnumerable<CreateOrderItemRequest> orderItems)
    {
        MerchantId = merchantId;
        PurchaseOrderNumber = purchaseOrderNumber;
        Destination = destination;
        OrderItems = orderItems;
    }

    public int MerchantId { get; }
    public string PurchaseOrderNumber { get; }
    public DestinationRequest Destination { get; }
    public IEnumerable<CreateOrderItemRequest> OrderItems { get; }

    public static Result<CreateOrderRequest> Create(
        int merchantId,
        string purchaseOrderNumber,
        string streetAddressOne,
        string streetAddressTwo,
        string locality,
        string region,
        string postalCode,
        string countryCode,
        IEnumerable<(string Sku, int Quantity)> items)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderNumber))
            return Error.Validation("purchaseOrderNumber is required");

        if (string.IsNullOrWhiteSpace(locality))
            return Error.Validation("purchaseOrderNumber is required");

        if (string.IsNullOrWhiteSpace(region))
            return Error.Validation("purchaseOrderNumber is required");

        if (string.IsNullOrWhiteSpace(postalCode))
            return Error.Validation("purchaseOrderNumber is required");

        if (string.IsNullOrWhiteSpace(countryCode))
            return Error.Validation("countryCode is required");

        if (items is null || !items.Any())
            return Error.Validation("items is required");

        if (items.Any(x => string.IsNullOrWhiteSpace(x.Sku)))
            return Error.Validation("each item must have a sku");

        if (items.Any(x => x.Quantity <= 0))
            return Error.Validation("each item must have a quantity greater than 0");

        return new CreateOrderRequest(
            merchantId,
            purchaseOrderNumber,
            new DestinationRequest(
                streetAddressOne, streetAddressTwo, locality, region, postalCode, countryCode),
            items.Select(i => new CreateOrderItemRequest(i.Sku, i.Quantity))
        );
    }
}