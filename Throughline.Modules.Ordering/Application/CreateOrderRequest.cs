namespace Throughline.Modules.Ordering.Application;

public sealed record CreateOrderRequest(
    int MerchantId,
    string PurchaseOrderNumber,
    string ReferenceNumber,
    IEnumerable<(string Sku, int Quantity)> Items,
    string StreetAddressOne,
    string? StreetAddressTwo,
    string City,
    string State,
    string PostalCode)
{
}