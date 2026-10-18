using System.Net;

namespace Throughline.Simulator.Orders;

/// <summary>
///     The 940 the customer's OMS posts to <c>/orders</c>. Defined here rather than shared with
///     Throughline, as an outside system's would be, so a contract change shows up as a failure.
/// </summary>
internal sealed record OrderRequest(
    string PurchaseOrderNumber,
    string ReferenceNumber,
    string StreetAddressOne,
    string? StreetAddressTwo,
    string City,
    string State,
    string PostalCode,
    IReadOnlyList<OrderRequestItem> Items);

internal sealed record OrderRequestItem(string Sku, int Quantity);

/// <summary>
///     What the OMS meant to send, so the response can be checked against what Throughline should do
/// </summary>
internal enum OrderKind
{
    New,
    IdenticalResend,
    ChangedResend,
    UnknownSku,
    MissingRequiredField,
    InvalidQuantity
}

internal sealed record GeneratedOrder(OrderKind Kind, OrderRequest Request)
{
    /// <summary>
    ///     Throughline's response to each kind of order, as traced in docs/design/simulator.md
    /// </summary>
    public HttpStatusCode ExpectedStatus => Kind switch
    {
        OrderKind.IdenticalResend => HttpStatusCode.OK,
        OrderKind.ChangedResend => HttpStatusCode.Conflict,
        OrderKind.MissingRequiredField => HttpStatusCode.BadRequest,

        // intake checks neither SKUs nor quantities; Inventory dead-letters these later
        OrderKind.UnknownSku or OrderKind.InvalidQuantity or OrderKind.New => HttpStatusCode.Created,
        _ => throw new InvalidOperationException($"No expected status for {Kind}")
    };
}
