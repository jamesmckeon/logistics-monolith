using System.Text.Json.Serialization;
using Throughline.Modules.Receiving.Domain.Inventory;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

internal sealed record ReceivedPallet
{
    public const string ExceptionOutcome = "receiving_exception";
    public const string OnHoldOutcome = "received_on_hold";
    public const string AvailableOutcome = "received_available";

    private ReceivedPallet(string lpn, string outcome)
    {
        Lpn = lpn;
        Outcome = outcome;
        Errors = [];
    }

    // Also the deserialization constructor for replaying a stored result; System.Text.Json requires each
    // parameter's type to match its property's type
    [JsonConstructor]
    private ReceivedPallet(string lpn, string outcome, IReadOnlyCollection<PalletError> errors)
    {
        Lpn = lpn;
        Outcome = outcome;
        Errors = errors;
    }

    public string Lpn { get; }
    public string Outcome { get; }
    public IReadOnlyCollection<PalletError> Errors { get; }

    public static ReceivedPallet Received(string lpn) => new(lpn, AvailableOutcome);

    public static ReceivedPallet HasExceptions(string lpn, IEnumerable<ReceivingExceptions> exceptions) =>
        new(lpn, ExceptionOutcome, exceptions.Select(PalletError.Exception).ToList().AsReadOnly());

    public static ReceivedPallet OnHold(string lpn, HoldReason holdReason) =>
        new(lpn, OnHoldOutcome, [PalletError.OnHold(holdReason)]);
}