using Throughline.Modules.Ordering.Application.Models;

namespace Throughline.Modules.Ordering.Application.Orders.Models;

public sealed record OrderModel(
    Guid OrderId,
    int MerchantId,
    string PurchaseOrderNumber,
    string ReferenceNumber,
    DestinationModel Destination,
    decimal DestinationSurcharge,
    decimal TotalCharges,
    IReadOnlyCollection<OrderLineModel> OrderLines)
{
}