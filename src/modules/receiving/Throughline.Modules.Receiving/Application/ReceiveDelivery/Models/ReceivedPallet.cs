namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed record ReceivedPallet(string Lpn, string Result, PalletError? Error)
{
    public static ReceivedPallet Received(string lpn) => new(lpn, "received_available", null);
    public static ReceivedPallet Exception(string lpn, PalletError error) => new(lpn, "receiving_exception", error);
    public static ReceivedPallet OnHold(string lpn, PalletError error) => new(lpn, "received_on_hold", error);
}