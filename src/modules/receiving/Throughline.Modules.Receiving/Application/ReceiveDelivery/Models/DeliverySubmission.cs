using System.Text.Json;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

/// <summary>
///     The normalized content a client submitted for a delivery, kept alongside the
///     <see cref="Domain.DeliveryReceipts.DeliveryReceipt" /> so a resubmission of the same <see cref="DeliveryId" />
///     can be recognized as equivalent (replay) or different (conflict).
/// </summary>
internal sealed class DeliverySubmission
{
    private DeliverySubmission(Guid deliveryId, int ownerId, string request, ReceiveDeliveryResult result)
    {
        DeliveryId = deliveryId;
        OwnerId = ownerId;
        Request = request;
        Result = result;
    }

    /// <summary>
    ///     The client-assigned delivery id; the same value as the delivery's id
    /// </summary>
    public Guid DeliveryId { get; }

    public int OwnerId { get; }

    /// <summary>
    ///     The comparable request body
    /// </summary>
    public string Request { get; }

    /// <summary>
    ///     The result of processing the request
    /// </summary>
    public ReceiveDeliveryResult Result { get; }

    public static DeliverySubmission Create(ReceiveDeliveryCommand command, ReceiveDeliveryResult result)
    {
        if (!command.Validate().Succeeded)
        {
            throw new ArgumentException("A DeliverySubmission instance cannot be created from an invalid command",
                nameof(command));
        }

        return new DeliverySubmission(
            command.DeliveryId, command.OwnerId,
            NormalizeRequest(command),
            result);
    }

    public static string NormalizeRequest(ReceiveDeliveryCommand command) =>
        JsonSerializer.Serialize(new
        {
            command.CarrierScac,
            command.BillOfLading,
            command.ContainerNumber,
            command.Items,
            command.ShipperName,
            command.TrailerNumber,
            command.DeliveryReference
        });
}