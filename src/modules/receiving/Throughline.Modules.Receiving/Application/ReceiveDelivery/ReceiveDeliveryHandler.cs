using Throughline.Common.Results;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed class ReceiveDeliveryHandler
{
    private readonly IDeliveryReceiptRepository _receiptRepository;
    private readonly IDeliverySubmissionStore _submissionStore;

    public ReceiveDeliveryHandler(
        IDeliveryReceiptRepository receiptRepository,
        IDeliverySubmissionStore submissionStore)
    {
        _receiptRepository = receiptRepository;
        _submissionStore = submissionStore;
    }


    public async Task<Result<ReceiveDeliveryResult>> ReceiveDeliveryAsync(ReceiveDeliveryCommand command,
        CancellationToken token)
    {
        var result = command.Validate();

        if (result.Succeeded)
        {
            return Result<ReceiveDeliveryResult>.FromFailureResult(result);
        }

        var existingSubmissions = await _submissionStore.GetSubmissionAsync(command.OwnerId, command.DeliveryId, token);

        if (existingSubmissions != null)
        {
            var request = DeliverySubmission.NormalizeRequest(command);
            if (request != existingSubmissions.Request)
            {
                return Result<ReceiveDeliveryResult>.Conflict($"A request for owner id {command.OwnerId}, " +
                                                              $"delivery id {command.DeliveryId} has already been processed " +
                                                              "with a different request body");
            }

            return existingSubmissions.Result;
        }

        throw new NotImplementedException();
    }
}