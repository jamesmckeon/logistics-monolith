using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Common.Results;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
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

    /*  TEST CASES
     *
     * submission exists with same content, returns response (verifies that repeats with duplicate receipt ids don't throw)
     * submission exists with different content, returns 409
     * existing LPN, received with exception
     * inactive hold reason, received with exception
     * all exception pallets should be received to exception location
     * valid hold reason, is received to hold location
     * valid pallet without hold, received to bulk? location
     * test concurrency?
     * if receipt has allocatable pallets, integration event should be published; else not
     */

    [Test]
    public async Task Post_MalformedRequest_ReturnsBadRequestWithErrors()
    {
        var command = new ReceiveDeliveryCommand(Guid.Empty, Guid.Empty, "test", "",
            "TESTT", " ", " ", "",
            [new SubmittedPallet(" ", " ", 0, " ", null, null, null)]);

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
        // TODO: seed existing lpn and then resubmit it to confirm SUT
        // TODO: also create pallet with hold reason that is inactive
        // creates pallet with exception

        var ownerId = 1;

        var requiresLotSku = new OwnerSku(ownerId, new("LotRequired"), Guid.NewGuid(), true, false);
        var requiresExpirationSku = new OwnerSku(ownerId, new("ExpRequired"), Guid.NewGuid(), false, true);
        var testSku = new OwnerSku(ownerId, new("TESTSKU"), Guid.NewGuid(), false, false);
        var testSku2 = new OwnerSku(ownerId, new("TESTSKU2"), Guid.NewGuid(), false, false);

        var location = new ReceivingLocation(new("TestLocation"), LocationTypes.Bulk);
        var exceptionLocation = new ReceivingLocation(new("ExceptionLocation"), LocationTypes.ReceivingException);

        var inactiveHold = new HoldReason(new("Inactive"), false);

        var invalidSku = TestPallet("LPN1", "InvalidSku", location.Id.Value, 2);
        var invalidLocation = TestPallet("LPN2", testSku.SkuCode.Value, "InvalidLocation", 3);

        var invalidHoldReason = TestPallet("LPN3", "TestSku", "TestLocation", 6, "InvalidHoldReason", null, null);
        var requiresLot = TestPallet("LPN4", requiresLotSku.SkuCode.Value, location.Id.Value, 7, null, null, null);
        var requiresExp = TestPallet("LPN5", requiresExpirationSku.SkuCode.Value, location.Id.Value, 8, null, null,
            null);
        var inactiveHoldReason = TestPallet(
            "LPN3", "TestSku", "TestLocation", 6, inactiveHold.ReasonCode.Value, null, null);

        var command = TestCommand([
            invalidSku, invalidLocation, invalidHoldReason, requiresLot, requiresExp, inactiveHoldReason
        ]);

        var scac = new CarrierScac(new(command.CarrierScac), 1, "Test Carrier");

        var lastReceiptNumber = 1;

        await SeedAsync(
            [requiresLotSku, requiresExpirationSku, testSku, testSku2],
            [location, exceptionLocation], [inactiveHold], scac, lastReceiptNumber);

        var response = await PostDeliveryReceipt(command, ownerId);
        var result = await GetResultFromResponse(response);

        PalletError expectedError(string lpn)
        {
            var exception = lpn switch
            {
                _ when lpn == invalidSku.Lpn => ReceivingExceptions.InvalidSkuCode,
                _ when lpn == inactiveHoldReason.Lpn => ReceivingExceptions.InvalidHoldReason,
                _ when lpn == invalidLocation.Lpn => ReceivingExceptions.InvalidLocation,
                _ when lpn == requiresLot.Lpn => ReceivingExceptions.LotNumberRequired,
                _ when lpn == requiresExp.Lpn => ReceivingExceptions.ExpirationDateRequired,
                _ when lpn == invalidHoldReason.Lpn => ReceivingExceptions.InvalidHoldReason
            };

            return PalletError.Exception(exception);
        }

        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result.ReceiptId, Is.EqualTo(command.ReceiptId));
            Assert.That(result.ReceiptNumber,
                Is.EqualTo(ReceiptNumber.FromSequence(lastReceiptNumber + 1).Value));

            foreach (var receivedPallet in result.Pallets)
            {
                var expectedException = expectedError(receivedPallet.Lpn);
                Assert.That(receivedPallet.Errors.Single().Code, Is.EqualTo(expectedException.Code));
                Assert.That(receivedPallet.Errors.Single().Description, Is.EqualTo(expectedException.Description));
            }
        });
    }

/*
    [Test]
    public async Task Post_OrderExistsWithDifferentContents_ReturnsConflict()
    {
        var existingCommand = TestCommand("Po1");
        var existingRecord = TestOrder(existingCommand);

        await SeedAsync(db =>
        {
            db.Orders.Add(existingRecord);
            return Task.CompletedTask;
        });

        var newCommand = TestCommand("Po2");

        var response = await PostReceipt(newCommand, existingRecord.OwnerId);
        var problemDetails = await GetFromResponse(response);
        Assert.That(problemDetails, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(problemDetails.Detail,
                Is.EqualTo(
                    $"Reference #{newCommand.ReferenceNumber} already identifies an order with different contents."));
        });
    }

    [Test]
    public async Task Post_OrderExists_ReturnsNotCreated()
    {
        var command = TestCommand();
        var orderRecord = TestOrder(command);

        await SeedAsync(db =>
        {
            db.Orders.Add(orderRecord);
            return Task.CompletedTask;
        });

        var response = await PostReceipt(command, orderRecord.OwnerId);
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result.OwnerId, Is.EqualTo(orderRecord.OwnerId));
            Assert.That(result.OrderId, Is.EqualTo(orderRecord.OrderId));
            Assert.That(result.OwnerReferenceNumber, Is.EqualTo(orderRecord.ReferenceNumber));
        });
    }


    [Test]
    public async Task Post_NewOrder_ReturnsCreatedAndFiresEvent()
    {
        var ownerId = 1;
        var command = TestCommand();

        var response = await PostReceipt(command, ownerId);
        var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(result.OwnerId, Is.EqualTo(ownerId));
            Assert.That(result.OwnerReferenceNumber, Is.EqualTo(command.ReferenceNumber));
        });
    }


    [Test]
    public async Task Post_ConcurrentSubmissions_CreatesOneResult()
    {
        const int ownerId = 1;
        const int concurrency = 2;
        var commands = Enumerable.Repeat(TestCommand(), concurrency).ToList(); // identical content + ref #

        var responses = await PostConcurrently(commands, ownerId);

        var bodies = await Task.WhenAll(
            responses.Select(r => r.Content.ReadFromJsonAsync<CreateOrderResponse>()));
        var distinctOrderIds = bodies.Select(b => b!.OrderId).Distinct().ToList();
        var rowCount = await CountOrdersAsync(ownerId, commands[0].ReferenceNumber);

        Assert.Multiple(() =>
        {
            Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.Created), Is.EqualTo(1));
            Assert.That(responses.Count(r => r.StatusCode == HttpStatusCode.OK), Is.EqualTo(concurrency - 1));
            Assert.That(distinctOrderIds, Has.Count.EqualTo(1)); // every response points at the one winner
            Assert.That(rowCount, Is.EqualTo(1)); // durable boundary held
        });
    }

    #endregion


    #region Get

    [Test]
    public async Task Get_OrderExists_ReturnsOk()
    {
        var command = TestCommand();
        var orderRecord = TestOrder(command);

        await SeedAsync(db =>
        {
            db.Orders.Add(orderRecord);
            return Task.CompletedTask;
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{OrderingExtensions.OrdersRoute}/{orderRecord.OrderId}");
        request.Headers.Add("owner_id", orderRecord.OwnerId.ToString());

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var model = await response.Content.ReadFromJsonAsync<OrderModel>();

        Assert.That(model, Is.Not.Null);

        var expectedDestination = new DestinationModel(
            orderRecord.StreetAddressOne, orderRecord.StreetAddressTwo,
            orderRecord.City, orderRecord.State, orderRecord.Zipcode);
        var expectedLines = orderRecord.OrderLines.Select(i => new OrderLineModel(i.SkuCode, i.Quantity));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(model.OwnerId, Is.EqualTo(orderRecord.OwnerId));
            Assert.That(model.ReferenceNumber, Is.EqualTo(command.ReferenceNumber));
            Assert.That(model.OrderId, Is.EqualTo(orderRecord.OrderId));
            Assert.That(model.PurchaseOrderNumber, Is.EqualTo(orderRecord.PurchaseOrderNumber));
            Assert.That(model.Destination, Is.EqualTo(expectedDestination));
            Assert.That(model.OrderLines, Is.EquivalentTo(expectedLines));
        });
    }

    [Test]
    public async Task Get_OrderNotFound_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();

        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{OrderingExtensions.OrdersRoute}/{orderId}");
        request.Headers.Add("owner_id", "1");

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Get_OrderExistsForDifferentOwner_ReturnsNotFound()
    {
        var command = TestCommand();
        var orderRecord = TestOrder(command);

        await SeedAsync(db =>
        {
            db.Orders.Add(orderRecord);
            return Task.CompletedTask;
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{OrderingExtensions.OrdersRoute}/{orderRecord.OrderId}");
        request.Headers.Add("owner_id", (orderRecord.OwnerId + 1).ToString()); // different owner

        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
*/

    #endregion

    #region Helpers

    private static ReceiveDeliveryCommand TestCommand(IEnumerable<SubmittedPallet> pallets) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "TestReference", "TestBol", "SCAC", "TestTrailer",
            "TestContainer", "TestShipper", pallets);

    private static SubmittedPallet TestPallet(string lpn, string skuCode, string locationId, int quantity = 1) =>
        new(lpn, skuCode, quantity, locationId, null, null, null);

    private static SubmittedPallet TestPallet(string lpn, string skuCode, string locationId, int quantity,
        string? holdReason, string? lotNumber, DateTime? expirationDate) =>
        new(lpn, skuCode, quantity, locationId, lotNumber, expirationDate, holdReason);


    // Fires all requests "at once": every task parks on the gate, then we release together.
    private async Task<IReadOnlyList<HttpResponseMessage>> PostConcurrently(
        IReadOnlyList<ReceiveDeliveryCommand> commands, int ownerId)
    {
        var gate = new TaskCompletionSource();
        var tasks = commands
            .Select(async cmd =>
            {
                await gate.Task; // park here until released
                return await PostDeliveryReceipt(cmd, ownerId); // reuses your existing helper
            })
            .ToArray();

        gate.SetResult(); // launch together
        return await Task.WhenAll(tasks);
    }

    private static async Task<ProblemDetails?> GetFromResponse(HttpResponseMessage response)
    {
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        var details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return details;
    }

    private static async Task<ReceiveDeliveryResult?> GetResultFromResponse(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        return await response.Content.ReadFromJsonAsync<ReceiveDeliveryResult>();
    }

    private async Task SeedAsync(
        IEnumerable<OwnerSku> skus,
        IEnumerable<ReceivingLocation> locations,
        IEnumerable<HoldReason> holdReasons,
        CarrierScac scac,
        int lastReceiptNumber = 0)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();

        var skuRecords = skus.Select(s => new SkuRecord
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


    private async Task ResetAsync()
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();

        await dbContext.Locations.ExecuteDeleteAsync();
        await dbContext.DeliveryReceipts.ExecuteDeleteAsync();
        await dbContext.DeliverySubmissions.ExecuteDeleteAsync();
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