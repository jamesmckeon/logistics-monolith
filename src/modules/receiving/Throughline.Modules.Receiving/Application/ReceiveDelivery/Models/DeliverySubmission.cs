using System.Text.Json;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;

/// <summary>
///     The normalized content a client submitted for a delivery, kept alongside the
///     <see cref="Domain.DeliveryReceipts.DeliveryReceipt" /> so a resubmission of the same <see cref="DeliveryId" />
///     can be recognized as equivalent (replay) or different (conflict).
/// </summary>
internal sealed class DeliverySubmission
{
    private DeliverySubmission(
        Guid deliveryId, int ownerId, string request, ReceiveDeliveryResult result, Guid? createdBy)
    {
        DeliveryId = deliveryId;
        OwnerId = ownerId;
        Request = request;
        Result = result;
        CreatedBy = createdBy;
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

    /// <summary>
    ///     The Entra object ID (<c>oid</c>) of the caller who submitted it; null only for submissions made before
    ///     it was recorded
    /// </summary>
    public Guid? CreatedBy { get; }

    public static DeliverySubmission FromCommandResult(
        int ownerId, ReceiveDeliveryCommand command, ReceiveDeliveryResult result, Guid createdBy)
    {
        if (!command.Validate().Succeeded)
        {
            throw new ArgumentException("A DeliverySubmission instance cannot be created from an invalid command",
                nameof(command));
        }

        return new DeliverySubmission(
            command.ReceiptId,
            ownerId,
            NormalizeRequest(command),
            result,
            createdBy);
    }

    public static string NormalizeRequest(ReceiveDeliveryCommand command) =>
        JsonSerializer.Serialize(new
        {
            command.CarrierScac,
            command.BillOfLading,
            command.ContainerNumber,
            Items = command.Pallets,
            command.ShipperName,
            command.TrailerNumber,
            command.DeliveryReference
        });
}