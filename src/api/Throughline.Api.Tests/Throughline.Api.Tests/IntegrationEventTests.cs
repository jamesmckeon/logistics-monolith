using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Modules.Inventory.Infrastructure.Orders;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Presentation;
using Wolverine.Tracking;
using InventoryOrder = Throughline.Modules.Inventory.Application.Models.OrderModel;

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

        CreateOrderResponse? result = null;

        await _testFactory.Services.ExecuteAndWaitAsync(async _ =>
        {
            var response = await PostOrder(createOrderCommand, ownerId);
            result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        });

        Assert.That(result, Is.Not.Null);

        var inventoryOrder = await _client.GetFromJsonAsync<InventoryOrder>
            ($"/inventory/orders?ownerId={ownerId}&orderId={result.OrderId}");

        Assert.That(inventoryOrder, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(inventoryOrder.OrderId, Is.EqualTo(result.OrderId));
            Assert.That(inventoryOrder.OwnerId, Is.EqualTo(result.OwnerId));
            Assert.That(inventoryOrder.AllocationStatus, Is.EqualTo("NotAllocated"));
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
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Orders.ExecuteDeleteAsync();
    }

    #region Helpers

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
                new CreateOrderCommandItem("TestSku", 1)
            ]);
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