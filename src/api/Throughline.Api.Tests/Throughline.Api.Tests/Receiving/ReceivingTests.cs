using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Domain.Common;
using Throughline.Modules.Receiving.Domain.DeliveryReceipts;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Domain.Shipments;
using Throughline.Modules.Receiving.Domain.Skus;
using Throughline.Modules.Receiving.Infrastructure.Db;
using Throughline.Modules.Receiving.Infrastructure.Db.Models;
using Throughline.Modules.Receiving.Presentation;

namespace Throughline.Api.Tests.Receiving;

[Category("Integration")]
internal class ReceivingTests : IntegrationTestsBase<ReceivingDbContext>
{
    private const string HoldLocationId = "TEST-HOLD";
    private const string ExceptionLocationId = "TEST-EXC";
    private const string BulkLocationId = "TEST-BULK";

    protected override IDictionary<string, string> AppSettings =>
        new Dictionary<string, string>
        {
            ["Receiving:DefaultLocations:0:LocationType"] = nameof(LocationTypes.Hold),
            ["Receiving:DefaultLocations:0:LocationId"] = HoldLocationId,
            ["Receiving:DefaultLocations:1:LocationType"] = nameof(LocationTypes.ReceivingException),
            ["Receiving:DefaultLocations:1:LocationId"] = ExceptionLocationId,
            ["Receiving:DefaultLocations:2:LocationType"] = nameof(LocationTypes.Bulk),
            ["Receiving:DefaultLocations:2:LocationId"] = BulkLocationId
        };

    private static ReceivingLocation HoldLocation => new(new(HoldLocationId), LocationTypes.Hold);

    private static ReceivingLocation ExceptionLocation =>
        new(new(ExceptionLocationId), LocationTypes.ReceivingException);

    private static ReceivingLocation BulkLocation => new(new(BulkLocationId), LocationTypes.Bulk);

    [TearDown]
    public async Task TearDown()
    {
        await ResetAsync();
    }

    #region Post

    [Test]
    public async Task Post_MalformedRequest_ReturnsBadRequestWithErrors()
    {
        var command = new ReceiveDeliveryCommand(Guid.Empty, Guid.Empty, "test", "",
            "TESTT", " ", " ", "",
            [new SubmittedPallet(" ", " ", 0, " ")]);

        var response = await PostDeliveryReceipt(command, 1);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problemDetails, Is.Not.Null);

        var errors = problemDetails.Errors
            .SelectMany(e => e.Value.Select(description => new FieldError(description, e.Key)))
            .ToArray();

        FieldError[] expectedErrors =
        [
            new("ReceiptId is required", "ReceiptId"),
            new("OperatorId is required", "OperatorId"),
            new("Either ContainerNumber or TrailerNumber is required", "TrailerNumber"),
            new("Either ContainerNumber or TrailerNumber is required", "ContainerNumber"),
            new("value must be 4 alpha characters", "CarrierScac"),
            new("Each pallet must have a non-blank sku", "Pallets"),
            new("Each pallet must have a non-blank lpn", "Pallets"),
            new("Each pallet must have a non-blank location ID", "Pallets"),
            new("Each pallet must have quantity greater than zero", "Pallets")
        ];

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(errors, Is.EquivalentTo(expectedErrors));
        });
    }

    [Test]
    public async Task Post_EmptyPallets_ReturnsBadRequestWithErrors()
    {
        var command = new ReceiveDeliveryCommand(Guid.NewGuid(), Guid.NewGuid(), "test", "test",
            "TEST", "test", "test", "", []);

        var response = await PostDeliveryReceipt(command, 1);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.That(problemDetails, Is.Not.Null);

        var errors = problemDetails.Errors
            .SelectMany(e => e.Value.Select(description => new FieldError(description, e.Key)))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(errors,
                Is.EqualTo(new[] { new FieldError("Pallets must contain at least one item", "Pallets") }));
        });
    }

    [Test]
    public async Task Post_PalletsWithExceptions_CreatesInvalidPallets()
    {
        var ownerId = 1;

        var requiresLotSku = new OwnerSku(ownerId, new("LotRequired"), Guid.NewGuid(), true, false);
        var requiresExpirationSku = new OwnerSku(ownerId, new("ExpRequired"), Guid.NewGuid(), false, true);
        var testSku = new OwnerSku(ownerId, new("TESTSKU"), Guid.NewGuid(), false, false);
        var testSku2 = new OwnerSku(ownerId, new("TESTSKU2"), Guid.NewGuid(), false, false);

        var location = new ReceivingLocation(new("TestLocation"), LocationTypes.Bulk);
        // create a second exception type location, to verify SUT stores pallets in default
        // exception location
        var otherExceptionLocation = new ReceivingLocation(new("OtherExceptionLocation"),
            LocationTypes.ReceivingException);

        var inactiveHold = new HoldReason(new("Inactive"), false);

        var invalidSku = TestPallet("LPN1", "InvalidSku", location.Id.Value, 2);
        var invalidLocation = TestPallet("LPN2", testSku.SkuCode.Value, "InvalidLocation", 3);

        var invalidHoldReason = TestPallet("LPN3", "TestSku", "TestLocation", 6, "InvalidHoldReason", null, null);
        var requiresLot = TestPallet("LPN4", requiresLotSku.SkuCode.Value, location.Id.Value, 7, null, null, null);
        var requiresExp = TestPallet("LPN5", requiresExpirationSku.SkuCode.Value, location.Id.Value, 8, null, null,
            null);
        var inactiveHoldReason = TestPallet(
            "LPN6", "TestSku", "TestLocation", 6, inactiveHold.ReasonCode.Value, null, null);

        var command = TestCommand([
            invalidSku, invalidLocation, invalidHoldReason, requiresLot, requiresExp, inactiveHoldReason
        ]);

        var scac = new CarrierScac(new(command.CarrierScac), 1, "Test Carrier");

        var lastReceiptNumber = 1;

        await SeedAsync(
            ownerId,
            [requiresLotSku, requiresExpirationSku, testSku, testSku2],
            [location, otherExceptionLocation],
            [inactiveHold],
            scac,
            lastReceiptNumber);

        var response = await PostDeliveryReceipt(command, ownerId);
        var result = await GetResultFromResponse(response);
        var receipt = await GetReceiptAsync(ownerId, command.ReceiptId);

        Assert.That(receipt, Is.Not.Null);

        PalletError expectedError(string lpn)
        {
            var exception = lpn switch
            {
                _ when lpn == invalidSku.Lpn => ReceivingExceptions.InvalidSkuCode,
                _ when lpn == inactiveHoldReason.Lpn => ReceivingExceptions.InvalidHoldReason,
                _ when lpn == invalidLocation.Lpn => ReceivingExceptions.InvalidLocation,
                _ when lpn == requiresLot.Lpn => ReceivingExceptions.LotNumberRequired,
                _ when lpn == requiresExp.Lpn => ReceivingExceptions.ExpirationDateRequired,
                _ when lpn == invalidHoldReason.Lpn => ReceivingExceptions.InvalidHoldReason,
                _ => throw new NotSupportedException()
            };

            return PalletError.Exception(exception);
        }

        Assert.That(result, Is.Not.Null);
        Assert.That(receipt, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result.ReceiptId, Is.EqualTo(command.ReceiptId));
            Assert.That(result.ReceiptNumber,
                Is.EqualTo(ReceiptNumber.FromSequence(lastReceiptNumber + 1).Value));
            Assert.That(result.Pallets.Count, Is.EqualTo(6));

            foreach (var receivedPallet in result.Pallets)
            {
                var receiptPallet =
                    receipt.InvalidPallets.Single(s => s.LicensePlateNumber.Value == receivedPallet.Lpn);
                var expectedException = expectedError(receivedPallet.Lpn);

                Assert.That(receiptPallet.Location, Is.EqualTo(ExceptionLocation));
                Assert.That(receivedPallet.Errors.Single().Code, Is.EqualTo(expectedException.Code));
                Assert.That(receivedPallet.Errors.Single().Description,
                    Is.EqualTo(expectedException.Description));
                Assert.That(receivedPallet.Outcome, Is.EqualTo(ReceivedPallet.ExceptionOutcome));
            }
        });
    }

    [Test]
    public async Task Post_LpnsExist_CreatesInvalidPalletsAndIncrementsReceipt()
    {
        var ownerId = 1;

        // create one processed/saved lpn that was received as valid/allocatable and one
        // that had an exception, to verify SUT considers both types of pallet
        var validLpn = "ValidLpn";
        var invalidLpn = "InvalidLpn";

        var locationId = "TestLocation";

        var validLpnPallet = new SubmittedPallet(validLpn, "ValidLpnSku", 1, locationId);
        var inValidLpnPallet = new SubmittedPallet(invalidLpn, "InValidLpnSku", 1, locationId);
        var firstCommand = TestCommand([validLpnPallet, inValidLpnPallet]);

        await SeedAsync(
            ownerId,
            firstCommand,
            [validLpnPallet, inValidLpnPallet]
        );

        var firstResponse = await PostDeliveryReceipt(firstCommand, ownerId);
        var firstResult = await GetResultFromResponse(firstResponse);

        var secondCommand = TestCommand([validLpnPallet, inValidLpnPallet]);

        var secondResponse = await PostDeliveryReceipt(secondCommand, ownerId);
        var secondResult = await GetResultFromResponse(secondResponse);

        Assert.That(firstResult, Is.Not.Null);
        Assert.That(secondResult, Is.Not.Null);

        var expectedException = PalletError.Exception(ReceivingExceptions.ExistingLpn);

        Assert.Multiple(() =>
        {
            Assert.That(secondResult.ReceiptNumber, Is.GreaterThan(firstResult.ReceiptNumber));

            foreach (var receivedPallet in secondResult.Pallets)
            {
                Assert.That(receivedPallet.Errors.Single().Code, Is.EqualTo(expectedException.Code));
                Assert.That(receivedPallet.Errors.Single().Description,
                    Is.EqualTo(expectedException.Description));
                Assert.That(receivedPallet.Outcome, Is.EqualTo(ReceivedPallet.ExceptionOutcome));
            }
        });
    }

    [Test]
    public async Task Post_PalletsWithHolds_CreatesHeldPallets()
    {
        var ownerId = 1;
        // seedAsync will create a hold reason with id/code "TestHoldReason"
        var pallet = new SubmittedPallet("TestLpn", "TestSku", 1, "TestLocation", null, null, "TestHoldReason");
        var command = TestCommand([pallet]);

        // verify SUT uses default hold location
        var otherHoldLocation = new ReceivingLocation(new("OTHERHOLDLOCATION"), LocationTypes.Hold);
        var testLocation = new ReceivingLocation(new(pallet.LocationId), LocationTypes.Bulk);

        await SeedAsync(
            ownerId,
            command,
            [pallet],
            [otherHoldLocation, testLocation]
        );

        var response = await PostDeliveryReceipt(command, ownerId);
        var result = await GetResultFromResponse(response);
        var receipt = await GetReceiptAsync(ownerId, command.ReceiptId);

        Assert.That(result, Is.Not.Null);
        Assert.That(receipt, Is.Not.Null);

        var resultPallet = result.Pallets.Single();
        var receiptPallet = receipt.Pallets.Single();

        Assert.Multiple(() =>
        {
            Assert.That(resultPallet.Outcome, Is.EqualTo(ReceivedPallet.OnHoldOutcome));
            Assert.That(receiptPallet.Location, Is.EqualTo(HoldLocation));
        });
    }

    [Test]
    public async Task Post_AvailablePallets_CreatesUnheldPallets()
    {
        var ownerId = 1;

        var palletOne = new SubmittedPallet("TestLpn", "TestSku", 1, "TestLocation");
        var palletTwo =
            new SubmittedPallet("TestLpn2", "TestSku2", 2, "TestLocation2", "test lot", new DateTime(2026, 1, 2));
        var command = TestCommand([palletOne, palletTwo]);

        await SeedAsync(
            ownerId,
            command,
            [palletOne, palletTwo]
        );

        var response = await PostDeliveryReceipt(command, ownerId);
        var result = await GetResultFromResponse(response);
        var receipt = await GetReceiptAsync(ownerId, command.ReceiptId);

        Assert.That(result, Is.Not.Null);
        Assert.That(receipt, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Pallets.Count, Is.EqualTo(2));

            foreach (var pallet in result.Pallets)
            {
                var receiptPallet = receipt.Pallets.Single(s => s.LicensePlateNumber.Value == pallet.Lpn);

                // Received to the default bulk location, not the location submitted with the pallet
                Assert.That(receiptPallet.Location, Is.EqualTo(BulkLocation));
                Assert.That(pallet.Outcome, Is.EqualTo(ReceivedPallet.AvailableOutcome));
                Assert.That(pallet.Errors, Is.Empty);
                Assert.That(new[]
                    {
                        new UpperCaseString(palletOne.Lpn).Value, new UpperCaseString(palletTwo.Lpn).Value
                    },
                    Does.Contain(pallet.Lpn));
            }
        });
    }

    [Test]
    public async Task Post_DuplicateRequests_ReturnsSameResponse()
    {
        var ownerId = 1;

        var pallet = new SubmittedPallet("TestLpn", "TestSku", 1, "TestLocation");

        var command = TestCommand([pallet]);

        await SeedAsync(
            ownerId,
            command,
            [pallet]
        );

        var firstResponse = await PostDeliveryReceipt(command, ownerId);
        var secondResponse = await PostDeliveryReceipt(command, ownerId);

        Assert.That(
            await secondResponse.Content.ReadAsStringAsync(),
            Is.EqualTo(await firstResponse.Content.ReadAsStringAsync()));
    }

    #endregion

    #region Helpers

    private static ReceiveDeliveryCommand TestCommand(IEnumerable<SubmittedPallet> pallets) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "TestReference", "TestBol", "SCAC", "TestTrailer",
            "TestContainer", "TestShipper", pallets);

    private static SubmittedPallet TestPallet(string lpn, string skuCode, string locationId, int quantity = 1) =>
        new(lpn, skuCode, quantity, locationId);

    private static SubmittedPallet TestPallet(string lpn, string skuCode, string locationId, int quantity,
        string? holdReason, string? lotNumber, DateTime? expirationDate) =>
        new(lpn, skuCode, quantity, locationId, lotNumber, expirationDate, holdReason);

    private static async Task<ReceiveDeliveryResult?> GetResultFromResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        return await response.Content.ReadFromJsonAsync<ReceiveDeliveryResult>();
    }

    private async Task<DeliveryReceipt?> GetReceiptAsync(int ownerId, Guid receiptId)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeliveryReceiptRepository>();

        return await repository.GetReceiptByIdAsync(ownerId, new UniqueId(receiptId), CancellationToken.None);
    }

    private async Task SeedAsync(
        int ownerId,
        IEnumerable<OwnerSku> skus,
        IEnumerable<ReceivingLocation> locations,
        IEnumerable<HoldReason> holdReasons,
        CarrierScac scac,
        int lastReceiptNumber = 1)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();

        var skuRecords = skus.DistinctBy(d => d.SkuCode).Select(s => new SkuRecord
        {
            IsExpirationTracked = s.IsExpirationTracked,
            IsLotTracked = s.IsLotTracked,
            OwnerId = s.OwnerId,
            SkuCode = s.SkuCode.Value,
            SkuId = s.Id
        });
        await dbContext.Skus.AddRangeAsync(skuRecords);

        await dbContext.ReceiptNumberCounters.AddAsync(new ReceiptNumberCounterRecord
        {
            OwnerId = ownerId,
            LastNumber = lastReceiptNumber
        });

        await dbContext.Carriers.AddAsync(new()
        {
            CarrierId = scac.CarrierId,
            CarrierName = scac.CarrierName,
            ScacCode = scac.ScacCode.Value
        });

        await dbContext.Locations.AddRangeAsync(
            locations.Concat([HoldLocation, BulkLocation, ExceptionLocation]));

        await dbContext.HoldReasons.AddRangeAsync(holdReasons);

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAsync(
        int ownerId,
        ReceiveDeliveryCommand command,
        IEnumerable<SubmittedPallet> pallets)
    {
        var palletsArray = pallets.ToArray();

        var skus = palletsArray.Select(p => new OwnerSku(ownerId, new(p.Sku), Guid.NewGuid(), false, false));
        var locations = palletsArray.Select(p => new ReceivingLocation(new(p.LocationId), LocationTypes.Bulk))
            .Distinct();

        var holdReasons = palletsArray.Any(a => !string.IsNullOrWhiteSpace(a.HoldReasonCode))
            ? palletsArray.Where(p => !string.IsNullOrWhiteSpace(p.HoldReasonCode))
                .Select(p => new HoldReason(new(p.HoldReasonCode!), true))
            : [new HoldReason(new("TestReason"), true)];

        await SeedAsync(ownerId, skus, locations, holdReasons, new(new(command.CarrierScac), 1, "Test Carrier"));
    }

    private async Task SeedAsync(
        int ownerId,
        ReceiveDeliveryCommand command,
        IEnumerable<SubmittedPallet> pallets,
        IEnumerable<ReceivingLocation> locations)
    {
        var palletsArray = pallets.ToArray();

        var skus = palletsArray.Select(p => new OwnerSku(ownerId, new(p.Sku), Guid.NewGuid(), false, false));

        var holdReasons = palletsArray.Any(a => !string.IsNullOrWhiteSpace(a.HoldReasonCode))
            ? palletsArray.Where(p => !string.IsNullOrWhiteSpace(p.HoldReasonCode))
                .Select(p => new HoldReason(new(p.HoldReasonCode!), true))
            : [new HoldReason(new("TestReason"), true)];

        await SeedAsync(ownerId, skus, locations, holdReasons, new(new(command.CarrierScac), 1, "Test Carrier"));
    }

    private async Task ResetAsync()
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();

        await dbContext.DeliveryReceipts.ExecuteDeleteAsync();
        await dbContext.DeliverySubmissions.ExecuteDeleteAsync();
        await dbContext.Locations.ExecuteDeleteAsync();

        await dbContext.Carriers.ExecuteDeleteAsync();
        await dbContext.Skus.ExecuteDeleteAsync();
        await dbContext.HoldReasons.ExecuteDeleteAsync();
        await dbContext.ReceiptNumberCounters.ExecuteDeleteAsync();
    }


    private async Task<HttpResponseMessage> PostDeliveryReceipt(
        ReceiveDeliveryCommand command, int ownerId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{ReceivingExtensions.ReceivingRoute}/receipts");
        request.Headers.Add("owner_id", ownerId.ToString());
        request.Content = JsonContent.Create(command);

        return await _client.SendAsync(request);
    }

    #endregion
}