using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Inventory.Presentation;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Presentation;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Infrastructure.Db;
using Throughline.Modules.Receiving.Infrastructure.Db.Models;
using Throughline.Modules.Receiving.Presentation;
using Wolverine.Tracking;

namespace Throughline.Api.Tests;

[Category("Integration")]
public sealed class IntegrationEventTests
{
    // The Receiving:DefaultLocations in appsettings.json; this factory doesn't override them
    private static readonly ReceivingLocation[] DefaultReceivingLocations =
    [
        new(new("HOLD-01"), LocationTypes.Hold),
        new(new("EXC-01"), LocationTypes.ReceivingException),
        new(new("BULK-01"), LocationTypes.Bulk)
    ];

    private HttpClient _client;
    private IntegrationEventsTestFactory _testFactory;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testFactory = new IntegrationEventsTestFactory();
        await _testFactory.InitializeAsync();
        _client = _testFactory.CreateClient();
        await _testFactory.ApplyMigrationsAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client?.Dispose();

        await _testFactory.DisposeAsync();
    }

    [Test]
    public async Task OrdersToInventory_OrderConfirmed_HandledByInventory()
    {
        var ownerId = 1;
        var createOrderCommand = TestCreateOrderCommand();

        await SeedInventorySkus(createOrderCommand, ownerId);

        CreateOrderResponse? result = null;

        await _testFactory.Services.ExecuteAndWaitAsync(async _ =>
        {
            var response = await PostOrder(createOrderCommand, ownerId);
            result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        });

        Assert.That(result, Is.Not.Null);

        var inventoryOrder = await GetInventoryOrder(result.OrderId, ownerId);

        Assert.That(inventoryOrder, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(inventoryOrder.OrderId, Is.EqualTo(result.OrderId));
            Assert.That(inventoryOrder.OwnerId, Is.EqualTo(result.OwnerId));
            Assert.That(inventoryOrder.AllocationStatus, Is.EqualTo("Confirmed"));
        });
    }

    [Test]
    public async Task ReceivingToInventory_PalletsReceived_InventoryCreatesReceipts()
    {
        var ownerId = 1;
        var now = AppDateTime.Now;

        await using var scope = _testFactory.Services.CreateAsyncScope();
        var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var skuWithInventory = new Sku(EntityId.Create(), ownerId, "SKU1");
        var otherSku = new Sku(EntityId.Create(), ownerId, "SKU2");

        await inventoryDbContext.Skus.AddRangeAsync(skuWithInventory, otherSku);
        await inventoryDbContext.SaveChangesAsync();

        var locationId = "TESTLOCATION";
        var holdReasonCode = "TESTHOLDCODE";

        var allocatablePallet = new SubmittedPallet("LPN1", skuWithInventory.Code, 1, locationId);
        // should be received as a receiving exception and not included in integration event
        var palletWithoutSku = new SubmittedPallet("LPN2", "SKU3", 1, locationId);
        // should be received as held and not included in integration event
        var heldPallet = new SubmittedPallet(
            "LPN3", skuWithInventory.Code, 1, locationId, null, null, holdReasonCode);

        var command = new ReceiveDeliveryCommand(
            Guid.NewGuid(), Guid.NewGuid(), "TestReference", "TestBol", "SCAC",
            "TestTrailer", "TestContainer", "TestShipper", [allocatablePallet, heldPallet, palletWithoutSku]);

        var receivingContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();

        var carrierScac = new CarrierRecord
        {
            CarrierId = 1,
            CarrierName = "Test Carrier",
            ScacCode = command.CarrierScac
        };

        await receivingContext.Carriers.AddAsync(carrierScac);

        var locations = command.Pallets
            .Select(p => new ReceivingLocation(new(p.LocationId), LocationTypes.Bulk))
            .Concat(DefaultReceivingLocations)
            .Distinct()
            .ToArray();
        await receivingContext.Locations.AddRangeAsync(locations);

        var skus = new[] { skuWithInventory, otherSku }
            .Select(p => new SkuRecord
            {
                OwnerId = ownerId,
                SkuCode = new(p.Code),
                SkuId = Guid.NewGuid(),
                IsExpirationTracked = false,
                IsLotTracked = false
            }).ToArray();
        await receivingContext.Skus.AddRangeAsync(skus);

        await receivingContext.SaveChangesAsync();

        await _testFactory.Services.ExecuteAndWaitAsync(async _ => { await PostDeliveryReceipt(command, ownerId); });

        var receipts = await inventoryDbContext.SkuReceipts.ToListAsync();
        var receipt = receipts.Single();

        Assert.Multiple(() =>
        {
            Assert.That(receipt.QuantityAllocated, Is.Zero);
            Assert.That(receipt.QuantityAvailable, Is.EqualTo(1));
            Assert.That(receipt.ReceivedOn, Is.GreaterThanOrEqualTo(now));
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        await ResetAsync();
    }

    private async Task ResetAsync()                            
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();

        var inventoryContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await inventoryContext.Orders.ExecuteDeleteAsync();
        await inventoryContext.SkuReceipts.ExecuteDeleteAsync();
        await inventoryContext.Skus.ExecuteDeleteAsync();

        var receivingContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();
        await receivingContext.DeliveryReceipts.ExecuteDeleteAsync();
        await receivingContext.DeliverySubmissions.ExecuteDeleteAsync();
        await receivingContext.Carriers.ExecuteDeleteAsync();
        await receivingContext.HoldReasons.ExecuteDeleteAsync();
        await receivingContext.Locations.ExecuteDeleteAsync();
        await receivingContext.ReceiptNumberCounters.ExecuteDeleteAsync();
        await receivingContext.Skus.ExecuteDeleteAsync();
    }


    #region Helpers

    private async Task PostDeliveryReceipt(
        ReceiveDeliveryCommand command, int ownerId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{ReceivingExtensions.ReceivingRoute}/receipts");
        request.Headers.Add("owner_id", ownerId.ToString());
        request.Content = JsonContent.Create(command);

        await _client.SendAsync(request);
    }


    private static CreateOrderCommand TestCreateOrderCommand() =>
        new(
            "Test PO",
            "testreference",
            "test address",
            null,
            "TestCity",
            "OR",
            "97211", [
                new CreateOrderCommandItem("TESTSKU", 1)
            ]);

    private async Task SeedInventorySkus(CreateOrderCommand command, int ownerId)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Skus.AddAsync(new Sku(EntityId.Create(), ownerId, command.Items.Single().Sku));
        await dbContext.SaveChangesAsync();
    }

    private async Task<OrderAllocationModel?> GetInventoryOrder(Guid orderId, int ownerId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{InventoryExtensions.InventoryRoute}/orders/{orderId}");
        request.Headers.Add("owner_id", ownerId.ToString());

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OrderAllocationModel>();
    }

    private async Task<HttpResponseMessage> PostOrder(CreateOrderCommand command, int ownerId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, OrderingExtensions.OrdersRoute);
        request.Headers.Add("owner_id", ownerId.ToString());
        request.Content = JsonContent.Create(command);

        return await _client.SendAsync(request);
    }

    #endregion
}