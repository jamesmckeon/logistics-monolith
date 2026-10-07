using Microsoft.Extensions.Options;
using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Application.Common;
using Throughline.Modules.Receiving.Application.Configuration;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Contracts.Events;
using Throughline.Modules.Receiving.Contracts.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Shipments;
using Throughline.Modules.Receiving.Domain.Skus;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed class ReceiveDeliveryHandler
{
    private readonly ICarrierProvider _carrierProvider;
    private readonly IOptions<AppConfiguration> _configuration;
    private readonly IPalletService _palletService;
    private readonly IDeliveryReceiptRepository _receiptRepository;
    private readonly ISkuProvider _skuProvider;
    private readonly IDeliverySubmissionStore _submissionStore;
    private readonly IUnitOfWork _unitOfWork;

    public ReceiveDeliveryHandler(
        IDeliveryReceiptRepository receiptRepository,
        IDeliverySubmissionStore submissionStore,
        IUnitOfWork unitOfWork,
        ISkuProvider skuProvider,
        ICarrierProvider carrierProvider,
        IPalletService palletService,
        IOptions<AppConfiguration> configuration)
    {
        _receiptRepository = receiptRepository;
        _submissionStore = submissionStore;
        _unitOfWork = unitOfWork;
        _skuProvider = skuProvider;
        _carrierProvider = carrierProvider;
        _configuration = configuration;
        _palletService = palletService;
    }


    public async Task<Result<ReceiveDeliveryResult>> ReceiveDeliveryAsync(
        int ownerId,
        ReceiveDeliveryCommand command,
        CancellationToken token)
    {
        var validationResult = command.Validate();

        if (!validationResult.Succeeded)
        {
            return Result<ReceiveDeliveryResult>.FromFailureResult(validationResult);
        }

        var existingSubmission = await _submissionStore.GetSubmissionAsync(
            ownerId, command.ReceiptId, token);

        if (existingSubmission != null)
        {
            var request = DeliverySubmission.NormalizeRequest(command);
            if (request != existingSubmission.Request)
            {
                return Result<ReceiveDeliveryResult>.Conflict($"A request for owner id {ownerId}, " +
                                                              $"receipt id {command.ReceiptId} has already been processed " +
                                                              "with a different request body");
            }

            return existingSubmission.Result;
        }

        var scacCode = new ScacCode(command.CarrierScac);
        var carrierScac = await _carrierProvider.GetCarrierScacByScacCodeAync(scacCode, token);

        if (carrierScac is null)
        {
            return Result<ReceiveDeliveryResult>.Validation($"Scac code '{command.CarrierScac}' not found");
        }

        var existingLpns = (await _receiptRepository.GetReceivedLpnsAsync(ownerId,
            command.Pallets.Select(p => new UpperCaseString(p.Lpn)), token)).ToArray();
        var locations = (await _receiptRepository.GetReceivingLocationsAsync(token))
            .ToArray();
        var defaultLocations = DefaultLocations.Resolve(_configuration.Value, locations);
        var holdReasons = (await _receiptRepository.GetHoldReasonsAsync(ownerId, token))
            .ToArray();

        var commandSkus = command.Pallets.Select(i => new UpperCaseString(i.Sku))
            .ToList();
        var skus = (await _skuProvider.GetSkusByOwnerSkuCodeAsync(
            ownerId, commandSkus, token)).ToArray();

        var serviceRequest = new PalletServiceRequest(
            ownerId, command.Pallets.ToArray(), skus, holdReasons, defaultLocations, existingLpns,
            locations);
        var serviceResult = _palletService.BuildPallets(serviceRequest);

        var shipment = CreateShipment(command, carrierScac);
        // Issued last, after every check that can reject the request: a number is spent once issued
        var receiptNumber = await _receiptRepository.NextReceiptNumberAsync(ownerId, token);

        var deliveryReceipt = new DeliveryReceipt(
            new UniqueId(command.ReceiptId),
            receiptNumber,
            ownerId,
            new(command.OperatorId),
            AppDateTime.Now,
            shipment,
            serviceResult.ValidPallets,
            serviceResult.InvalidPallets,
            AppDateTime.Now);

        var integrationEvent = ToEvent(deliveryReceipt);

        if (integrationEvent != null)
        {
            await _receiptRepository.AddAsync(deliveryReceipt, integrationEvent, token);
        }
        else
        {
            await _receiptRepository.AddAsync(deliveryReceipt, token);
        }

        var result = ToResult(deliveryReceipt);

        var submission = DeliverySubmission.FromCommandResult(ownerId, command, result);
        _submissionStore.Add(submission);

        await _unitOfWork.SaveChangesAsync(token);

        return result;
    }

    private ReceiveDeliveryResult ToResult(DeliveryReceipt receipt)
    {
        var pallets = receipt.Pallets.Select(p =>
                p.IsHeld
                    ? ReceivedPallet.OnHold(p.LicensePlateNumber.Value, p.HoldReason)
                    : ReceivedPallet.Received(p.LicensePlateNumber.Value))
            .ToList();

        pallets.AddRange(receipt.InvalidPallets.Select(ip =>
            ReceivedPallet.HasExceptions(ip.LicensePlateNumber.Value, ip.Exceptions)));

        return new ReceiveDeliveryResult(receipt.Id.Value, receipt.ReceiptNumber.Value, pallets);
    }

    private static Shipment CreateShipment(ReceiveDeliveryCommand command, CarrierScac carrierScac)
    {
        TrimmedString? toNullString(string? val) => string.IsNullOrWhiteSpace(val) ? null : new(val);

        return new(
            carrierScac,
            toNullString(command.BillOfLading),
            new(command.ShipperName),
            toNullString(command.TrailerNumber),
            toNullString(command.ContainerNumber),
            new(command.DeliveryReference));
    }

    /// <summary>
    ///     Creates an event instance based on the provided <c>DeliveryReceipt</c>.
    /// </summary>
    /// <param name="receipt"></param>
    /// <returns> An event instance if at least one pallet on the receipt has allocatable inventory</returns>
    private static AllocatablePalletsIntegrationEvent? ToEvent(DeliveryReceipt receipt)
    {
        if (!receipt.Pallets.Any(a => a.IsAllocatable))
        {
            return null;
        }

        return new(receipt.OwnerId, receipt.Id.Value, receipt.ReceiptNumber.Value, receipt.ReceivedOn.Value,
            receipt.Pallets.Where(p => p.IsAllocatable)
                .Select(p => new AllocatablePalletModel(
                    p.LicensePlateNumber.Value,
                    p.OwnerSku.SkuCode.Value,
                    p.Quantity,
                    p.Location.Id.Value))
                .ToList()
                .AsReadOnly());
    }
}