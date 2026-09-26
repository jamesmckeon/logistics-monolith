using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Inventory.Presentation;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Presentation;
using Wolverine.Tracking;

namespace Throughline.Api.Tests;

[Category("Integration")]
public sealed class IntegrationEventTests
{
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

    private async Task<InventoryDbContext> GetDbContext()
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    }

    private static CreateOrderCommand TestCreateOrderCommand()
    {
        return new CreateOrderCommand(
            "Test PO",
            "testreference",
            "test address",
            null,
            "TestCity",
            "OR",
            "97211", [
                new CreateOrderCommandItem("TESTSKU", 1)
            ]);
    }

    private async Task SeedSkus(CreateOrderCommand command, int ownerId)
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