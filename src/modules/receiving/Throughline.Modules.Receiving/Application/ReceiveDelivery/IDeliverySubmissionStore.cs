namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal interface IDeliverySubmissionStore
{
    Task<DeliverySubmission?> GetSubmissionAsync(int ownerId, Guid deliveryId, CancellationToken token);

    void Add(DeliverySubmission submission);
}
