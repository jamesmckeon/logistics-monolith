using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Application.Common;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Contracts.Events;
using Throughline.Modules.Receiving.Contracts.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Domain.Shipments;
using Throughline.Modules.Receiving.Domain.Skus;

namespace Throughline.Modules.Receiving.Application.ReceiveDelivery;

internal sealed class ReceiveDeliveryHandler
{
    private readonly ICarrierProvider _carrierProvider;
    private readonly IDefaultLocationsProvider _locationsProvider;
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
        IDefaultLocationsProvider locationsProvider)
    {
        _receiptRepository = receiptRepository;
        _submissionStore = submissionStore;
        _unitOfWork = unitOfWork;
        _skuProvider = skuProvider;
        _carrierProvider = carrierProvider;
        _locationsProvider = locationsProvider;
    }


    public async Task<Result<ReceiveDeliveryResult>> ReceiveDeliveryAsync(ReceiveDeliveryCommand command,
        CancellationToken token)
    {
        var validationResult = command.Validate();

        if (!validationResult.Succeeded)
        {
            return Result<ReceiveDeliveryResult>.FromFailureResult(validationResult);
        }

        var existingSubmission = await _submissionStore.GetSubmissionAsync(
            command.OwnerId, command.ReceiptId, token);

        if (existingSubmission != null)
        {
            var request = DeliverySubmission.NormalizeRequest(command);
            if (request != existingSubmission.Request)
            {
                return Result<ReceiveDeliveryResult>.Conflict($"A request for owner id {command.OwnerId}, " +
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

        var existingLpns = (await _receiptRepository.GetReceivedLpnsAsync(command.OwnerId,
            command.Pallets.Select(p => new UpperCaseString(p.Lpn)), token)).ToArray();
        var locations = (await _receiptRepository.GetReceivingLocationsAsync(token))
            .ToArray();
        var defaultLocations = await _locationsProvider.GetDefaultLocationsAsync(token);
        var holdReasons = (await _receiptRepository.GetHoldReasonsAsync(command.OwnerId, token))
            .ToArray();

        var commandSkus = command.Pallets.Select(i => new UpperCaseString(i.Sku))
            .ToList();
        var skus = (await _skuProvider.GetSkusByOwnerSkuCodeAsync(
            command.OwnerId, commandSkus, token)).ToArray();

        var invalidPallets = GetInvalidPallets(
            command, skus, existingLpns, locations, holdReasons, defaultLocations);

        var validPallets = command.Pallets.Where(p =>
                invalidPallets.All(ep => ep.LicensePlateNumber != new UpperCaseString(p.Lpn)))
            .ToArray();
        var pallets = GetValidPallets(validPallets, skus, holdReasons, defaultLocations);

        var shipment = CreateShipment(command, carrierScac);
        var lastReceiptNumber = await _receiptRepository.GetLastReceiptNumberAsync(command.OwnerId, token);

        var deliveryReceipt = new DeliveryReceipt(
            command.ReceiptId, ReceiptNumber.Create(lastReceiptNumber), command.OwnerId, command.OperatorId,
            AppDateTime.Now, shipment, pallets, invalidPallets);

        await _receiptRepository.AddAsync(deliveryReceipt, ToEvent(deliveryReceipt), token);

        var result = ToResult(deliveryReceipt);

        var submission = DeliverySubmission.FromCommandResult(command, result);
        _submissionStore.Add(submission);

        await _unitOfWork.SaveChangesAsync(token);

        return result;
    }

    private IEnumerable<Pallet> GetValidPallets(
        ReceiveDeliveryCommandItem[] validPallets,
        OwnerSku[] skus,
        HoldReason[] holdReasons,
        DefaultLocations defaultLocations)
    {
        var pallets = new List<Pallet>();

        foreach (var validPallet in validPallets)
        {
            var sku = skus.Single(s => s.SkuCode == new UpperCaseString(validPallet.Sku));
            var holdReason = string.IsNullOrWhiteSpace(validPallet.HoldReasonCode)
                ? null
                : holdReasons.Single(hr => hr.ReasonCode == new UpperCaseString(validPallet.HoldReasonCode));
            var location = holdReason is null
                ? defaultLocations.BulkLocation
                : defaultLocations.HoldLocation;
            var expiresOn = validPallet.Expires is null ? null : new AppDateTime(validPallet.Expires.Value);
            var pallet = new Pallet(UniqueId.Create(), new(validPallet.Lpn), sku, validPallet.Quantity, location,
                holdReason, expiresOn, validPallet.LotNumber);

            pallets.Add(pallet);
        }

        return pallets;
    }

    private IEnumerable<InvalidPallet> GetInvalidPallets(
        ReceiveDeliveryCommand command,
        OwnerSku[] allSkus,
        ReceivedLpn[] existingLpns,
        ReceivingLocation[] allLocations,
        HoldReason[] holdReasons,
        DefaultLocations defaultLocations)
    {
        var invalidPallets = new List<InvalidPallet>();

        foreach (var pallet in command.Pallets)
        {
            var exceptions = new List<ReceivingExceptions>();
            var sku = allSkus.FirstOrDefault(a => a.SkuCode == new UpperCaseString(pallet.Sku));

            if (sku is null)
            {
                exceptions.Add(ReceivingExceptions.InvalidSkuCode);
            }
            else
            {
                if (sku.IsLotTracked && string.IsNullOrWhiteSpace(pallet.LotNumber))
                {
                    exceptions.Add(ReceivingExceptions.LotNumberRequired);
                }

                if (sku.IsExpirationTracked && pallet.Expires == null)
                {
                    exceptions.Add(ReceivingExceptions.ExpirationDateRequired);
                }
            }


            if (existingLpns.Any(a => a.Lpn == new UpperCaseString(pallet.Lpn)))
            {
                exceptions.Add(ReceivingExceptions.DuplicateLpn);
            }

            if (allLocations.All(all => all.Id != new UpperCaseString(pallet.LocationId)))
            {
                exceptions.Add(ReceivingExceptions.InvalidLocation);
            }

            if (!string.IsNullOrWhiteSpace(pallet.HoldReasonCode) &&
                holdReasons.All(a => a.ReasonCode != new UpperCaseString(pallet.HoldReasonCode)))
            {
                exceptions.Add(ReceivingExceptions.InvalidHoldReason);
            }

            if (exceptions.Any())
            {
                var exceptedPallet = new InvalidPallet(
                    UniqueId.Create(),
                    command.OwnerId,
                    new(pallet.Sku),
                    new(pallet.Lpn),
                    pallet.Quantity,
                    defaultLocations.ExceptionLocation,
                    pallet.LocationId,
                    exceptions.ToArray());

                invalidPallets.Add(exceptedPallet);
            }
        }

        return invalidPallets;
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

        return new ReceiveDeliveryResult(receipt.Id, receipt.ReceiptNumber.Value, pallets);
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

        return new(receipt.OwnerId, receipt.Id, receipt.ReceiptNumber.Value, receipt.ReceivedOn.Value,
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