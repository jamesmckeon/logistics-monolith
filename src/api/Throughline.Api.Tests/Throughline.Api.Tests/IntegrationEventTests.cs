using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Api.Tests.Inventory;
using Throughline.Api.Tests.Ordering;
using Throughline.Common.Models;
using Throughline.Common.Presentation.Auth;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Contracts.Events;
using Throughline.Modules.Ordering.Contracts.Models;
using Throughline.Modules.Ordering.Presentation;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Domain.Inventory;
using Throughline.Modules.Receiving.Domain.Locations;
using Throughline.Modules.Receiving.Infrastructure.Db;
using Throughline.Modules.Receiving.Infrastructure.Db.Models;
using Throughline.Modules.Receiving.Presentation;
using Wolverine;
using Wolverine.Runtime;
using Wolverine.Runtime.Serialization;
using Wolverine.Tracking;
using OrderingOrdersDbContext = Throughline.Modules.Ordering.Infrastructure.Orders.OrdersDbContext;

namespace Throughline.Api.Tests;

[Category("Integration")]
internal sealed class IntegrationEventTests : IntegrationTestsBase
{
    // The Receiving:DefaultLocations in appsettings.json; this class doesn't override them
    private static readonly ReceivingLocation[] DefaultReceivingLocations =
    [
        new(new("HOLD-01"), LocationTypes.Hold),
        new(new("EXC-01"), LocationTypes.ReceivingException),
        new(new("BULK-01"), LocationTypes.Bulk)
    ];

    // These tests span all three modules (e.g. POST to Ordering -> OrderConfirmed -> Inventory),
    // so every schema must exist.
    protected override async Task MigrateAsync()
    {
        await _testFactory.MigrateAsync<OrderingOrdersDbContext>();
        await _testFactory.MigrateAsync<InventoryDbContext>();
        await _testFactory.MigrateAsync<ReceivingDbContext>();
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
            var response = await OrderingTests.PostOrderAsync(_client, createOrderCommand, ownerId);
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

    // A retry, dead-letter replay or inbox recovery rebuilds the message from its stored body with the
    // host's serializer, rather than handing the handler the published instance
    [Test]
    public void Deserialize_PublishedEvent_KeepsOccurredOnUtc()
    {
        var occurredOn = new DateTimeOffset(2026, 11, 20, 9, 30, 0, TimeSpan.Zero);
        var published = new OrderConfirmedIntegrationEvent(
            occurredOn, 1, Guid.CreateVersion7(), occurredOn, [new OrderLineEventModel("SKU1", 1)]);
        var serializer = HostSerializer();
        var stored = serializer.Write(new Envelope(published));

        var rebuilt = (OrderConfirmedIntegrationEvent)serializer.ReadFromData(
            typeof(OrderConfirmedIntegrationEvent), new Envelope { Data = stored });

        Assert.That(rebuilt.OccurredOnUtc, Is.EqualTo(published.OccurredOnUtc));
    }

    // The body stored before IntegrationEventBase took OccurredOnUtc as a constructor parameter, which also
    // carries the Id the base no longer has; those messages may still be in the outbox or the dead letters
    [Test]
    public void Deserialize_StoredWithPreviousBaseShape_KeepsStoredOccurredOnUtc()
    {
        var id = Guid.Parse("019a9f3c-5b2e-7c41-9d3a-2f6e8b1c4d70");
        var occurredOn = new DateTimeOffset(2026, 11, 20, 9, 30, 0, TimeSpan.Zero);
        var stored = Encoding.UTF8.GetBytes(
            $$"""
              {
                "ownerId": 1,
                "orderId": "019a9f3c-5b2e-7c41-9d3a-2f6e8b1c4d71",
                "createdOn": "2026-11-20T09:30:00+00:00",
                "lines": [{ "skuCode": "SKU1", "quantity": 1 }],
                "id": "{{id}}",
                "occurredOnUtc": "2026-11-20T09:30:00+00:00"
              }
              """);

        var rebuilt = (OrderConfirmedIntegrationEvent)HostSerializer()
            .ReadFromData(typeof(OrderConfirmedIntegrationEvent), new Envelope { Data = stored });

        Assert.Multiple(() =>
        {
            Assert.That(rebuilt.OccurredOnUtc, Is.EqualTo(occurredOn));
            Assert.That(rebuilt.OwnerId, Is.EqualTo(1));
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

    private IMessageSerializer HostSerializer() =>
        _testFactory.Services.GetRequiredService<IWolverineRuntime>().Options.DefaultSerializer;

    private async Task PostDeliveryReceipt(
        ReceiveDeliveryCommand command, int ownerId)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            $"{ReceivingExtensions.ReceivingRoute}/receipts",
            ownerId,
            ThroughlineRoles.Receiving,
            ThroughlineRoles.OwnerRole(ownerId));

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
        using var request = CreateRequest(
            HttpMethod.Get, InventoryTests.GetAllocatedOrderRoute(orderId), ownerId,
            ThroughlineRoles.OwnerRole(ownerId), ThroughlineRoles.OutboundPlanning);

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OrderAllocationModel>();
    }

    #endregion
}