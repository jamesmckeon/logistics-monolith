using Throughline.Common.Results;
using Throughline.Modules.Receiving.Application.Common;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;
using Throughline.Modules.Receiving.Domain.Shipments;
using Throughline.Modules.Receiving.Domain.Skus;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed class ReceiveDeliveryHandler
{
    private readonly ICarrierProvider _carrierProvider;
    private readonly IDeliveryReceiptRepository _receiptRepository;
    private readonly ISkuProvider _skuProvider;
    private readonly IDeliverySubmissionStore _submissionStore;
    private readonly IUnitOfWork _unitOfWork;

    public ReceiveDeliveryHandler(
        IDeliveryReceiptRepository receiptRepository,
        IDeliverySubmissionStore submissionStore,
        IUnitOfWork unitOfWork,
        ISkuProvider skuProvider,
        ICarrierProvider carrierProvider)
    {
        _receiptRepository = receiptRepository;
        _submissionStore = submissionStore;
        _unitOfWork = unitOfWork;
        _skuProvider = skuProvider;
        _carrierProvider = carrierProvider;
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

        var scacCodeResult = ScacCode.Create(command.CarrierScac);

        if (!scacCodeResult.Succeeded)
        {
            throw new ArgumentException("command.CarrierScac is invalid", nameof(command.CarrierScac));
        }

        var carrierScac = await _carrierProvider.GetCarrierScacByScacCodeAync(scacCodeResult.Value, token);

        if (carrierScac is null)
        {
            throw new NotImplementedException("create excepted deliveryreceipt: scaccode not in system");
        }

        if (!carrierScac.CarrierName.Equals(command.CarrierName.Trim()))
        {
            throw new NotImplementedException("create excepted deliveryreceipt: carrier name doesn't match system's");
        }

        var commandSkus = command.Items.Select(i => new UpperCaseString(i.Sku))
            .ToList();
        var skus = await _skuProvider.GetSkusByOwnerSkuCodeAsync(
            command.OwnerId, commandSkus, token);

        var invalidSkus = commandSkus.Where(w => skus.All(all => !all.Equals(w)))
            .ToList();
        if (invalidSkus.Any())
        {
            throw new NotImplementedException("create excepted deliveryreceipt: skus not found");
        }

        // create invalidapallets for those items in command that have an invalid hold reason
        // create invalidpallets for those items in command in that have a hold reason
        // create valid pallets for the rest of items
        // create new DeliverySubmission
        // create new DeliveryReceipt
        // save changes and handle race condition if it exists
        // there should only be one for both the submission and the aggregate, not one vs the other
        // bc UOW will commit both in a transaction


        throw new NotImplementedException();
    }
}