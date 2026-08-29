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
    IEnumerable<(string Sku, int Quantity)> Items);