using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Inventory.Presentation;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Presentation;
using Throughline.Modules.Receiving.Application.ReceiveDelivery.Models;
using Throughline.Modules.Receiving.Domain.Common;
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

        await SeedSkus(createOrderCommand, ownerId);

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
    public async Task ReceivingToInventory_AllocatablePalletReceived_InventoryCreatesReceipt()
    {
        var ownerId = 1;
        var now = AppDateTime.Now;

        var pallet = new SubmittedPallet("TestLpn", "TestSku", 99, "TestLocation");

        var sku = new Sku(EntityId.Create(), ownerId, pallet.Sku);
        await SeedSkus([sku]);

        // Receiving keeps its own copy of the sku; the same identity as Inventory's
        await SeedReceivingSkus([
            new SkuRecord
            {
                SkuId = sku.Id.Value,
                OwnerId = ownerId,
                SkuCode = new UpperCaseString(pallet.Sku).Value,
                IsLotTracked = false,
                IsExpirationTracked = false
            }
        ]);

        var receivingLocation = new ReceivingLocation(new(pallet.LocationId), LocationTypes.Bulk);
        await SeedReceivingLocations([receivingLocation, .. DefaultReceivingLocations]);

        var command = new ReceiveDeliveryCommand(
            Guid.NewGuid(), Guid.NewGuid(), "TestReference", "TestBol", "SCAC",
            "TestTrailer", "TestContainer", "TestShipper", [pallet]);

        await SeedCarrier(new CarrierRecord
        {
            CarrierId = 1,
            CarrierName = "Test Carrier",
            ScacCode = command.CarrierScac
        });

        ReceiveDeliveryResult? result = null;

        await _testFactory.Services.ExecuteAndWaitAsync(async _ => { await PostDeliveryReceipt(command, ownerId); });

        var inventory = (await GetAvailableInventory(sku.Id))
            .ToArray();
        var skuReceipt = inventory.Single();

        Assert.Multiple(() =>
        {
            Assert.That(skuReceipt.SkuId, Is.EqualTo(sku.Id));
            Assert.That(skuReceipt.QuantityAllocated, Is.Zero);
            Assert.That(skuReceipt.QuantityAvailable, Is.EqualTo(pallet.Quantity));
            Assert.That(skuReceipt.ReceivedOn, Is.GreaterThanOrEqualTo(now));
        });
    }

    [Test]
    public Task ReceivingToInventory_NoAllocatablePalletsReceived_DoesntCreateInventory() =>
        throw new NotImplementedException();

    [TearDown]
    public async Task TearDown()
    {
        await ResetAsync();
    }

    private async Task ResetAsync()
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Orders.ExecuteDeleteAsync();
    }

    #region Helpers

    private async Task<HttpResponseMessage> PostDeliveryReceipt(
        ReceiveDeliveryCommand command, int ownerId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{ReceivingExtensions.ReceivingRoute}/receipts");
        request.Headers.Add("owner_id", ownerId.ToString());
        request.Content = JsonContent.Create(command);

        return await _client.SendAsync(request);
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

    private async Task SeedSkus(CreateOrderCommand command, int ownerId)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Skus.AddAsync(new Sku(EntityId.Create(), ownerId, command.Items.Single().Sku));
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedSkus(IEnumerable<Sku> skus)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Skus.AddRangeAsync(skus);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedReceivingSkus(IEnumerable<SkuRecord> skus)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();
        await dbContext.Skus.AddRangeAsync(skus);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedCarrier(CarrierRecord carrier)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();
        await dbContext.Carriers.AddAsync(carrier);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedReceivingLocations(IEnumerable<ReceivingLocation> locations)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReceivingDbContext>();
        await dbContext.Locations.AddRangeAsync(locations);
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

    private async Task<IEnumerable<SkuReceipt>> GetAvailableInventory(EntityId skuId)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();

        return await repository.GetAvailableInventoryAsync([skuId], CancellationToken.None);
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