using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed record ReceivedPallet
{
    private ReceivedPallet(string lpn, string outcome)
    {
        Lpn = lpn;
        Outcome = outcome;
        Errors = [];
    }

    private ReceivedPallet(string lpn, string outcome, IEnumerable<PalletError> errors)
    {
        Lpn = lpn;
        Outcome = outcome;
        Errors = errors.ToList().AsReadOnly();
    }

    public string Lpn { get; }
    public string Outcome { get; }
    public IReadOnlyCollection<PalletError> Errors { get; }

    public static ReceivedPallet Received(string lpn) => new(lpn, "received_available");

    public static ReceivedPallet HasExceptions(string lpn, IEnumerable<ReceivingExceptions> exceptions) =>
        new(lpn, "receiving_exception", exceptions.Select(PalletError.Exception));

    public static ReceivedPallet OnHold(string lpn, HoldReason holdReason) =>
        new(lpn, "received_on_hold", [PalletError.OnHold(holdReason)]);
}