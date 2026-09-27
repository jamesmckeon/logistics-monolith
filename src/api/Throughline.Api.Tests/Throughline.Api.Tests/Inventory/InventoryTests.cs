using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Application.AllocateOrders;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Inventory.Presentation;

namespace Throughline.Api.Tests.Inventory;

[Category("Integration")]
public class InventoryTests
{
    private HttpClient _client;
    private InventoryTestFactory _testFactory;
    private List<Sku> TestSkus { get; set; }
    private Owner PartialAllocationOwner { get; set; }
    private Owner ShipCompleteOwner { get; set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _testFactory = new();
        await _testFactory.InitializeAsync();

        _client = _testFactory.CreateClient();
        await _testFactory.ApplyMigrationsAsync();

        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        TestSkus = Enumerable.Range(1, 5).Select(i => new Sku(EntityId.Create(), 1, $"SKU{i}"))
            .ToList();
        await dbContext.Skus.AddRangeAsync(TestSkus);

        PartialAllocationOwner = new Owner(1, AllocationPolicies.Partial);
        dbContext.Owners.Add(PartialAllocationOwner);
        ShipCompleteOwner = new Owner(2, AllocationPolicies.ShipComplete);
        dbContext.Owners.Add(ShipCompleteOwner);

        await dbContext.SaveChangesAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client?.Dispose();
        if (_testFactory is not null)
        {
            await _testFactory.DisposeAsync();
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        await ResetAsync();
    }

    [Test]
    public async Task Post_EmptyRequest_ReturnsInvalidRequest()
    {
        var response = await PostOrderAllocationsAsync(new AllocateOrdersCommand([]), 1);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var errors = await GetBadRequestErrors(response);
        Assert.That(errors, Is.Not.Null);

        var error = errors.Single();
        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.EqualTo(AllocationError.OrderIdsEmptyCode));
            Assert.That(error.Description, Is.EqualTo("command must contain at least one order id"));
        });
    }


    [Test]
    public async Task Post_DuplicateOrderIds_ReturnsInvalidRequest()
    {
        var orderId = Guid.CreateVersion7();
        var response = await PostOrderAllocationsAsync(
            new AllocateOrdersCommand([orderId, orderId]), 1);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var errors = await GetBadRequestErrors(response);
        Assert.That(errors, Is.Not.Null);

        var error = errors.Single();
        var expectedMessage = $"The request contains the following duplicate order ids: {orderId}";

        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.EqualTo(AllocationError.DuplicateOrderIdsCode));
            Assert.That(error.Description, Is.EqualTo(expectedMessage));
        });
    }

    [Test]
    public async Task Post_OrderAllocating_ReturnsExpectedError()
    {
        var orderId = Guid.CreateVersion7();
        var order = CreateOrder(PartialAllocationOwner, orderId, CreateLine(TestSkus.First()));

        var receipts = Enumerable.Range(1, 3).Select(i => CreateSkuReceipt(order.OrderLines.Single().SkuId, i));

        await SeedOrdersAndReceiptsAsync(receipts, order);

        var command = new AllocateOrdersCommand([orderId]);
        var requests = Enumerable.Range(0, 3)
            .Select(_ => PostOrderAllocationsAsync(command, PartialAllocationOwner.Id));

        var responses = await Task.WhenAll(requests);
        Assert.That(responses.Select(r => r.StatusCode), Is.All.EqualTo(HttpStatusCode.OK));

        var results = await Task.WhenAll(responses.Select(GetResult));
        Assert.That(results, Is.All.Not.Null);

        var orderResults = results.SelectMany(r => r!.Orders)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(orderResults.Count, Is.EqualTo(3));
            Assert.That(
                orderResults.Count(r =>
                    r.Errors.Any() && r!.Errors.Single().Code == AllocationError.OrderAllocatingCode),
                Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Post_ShipCompleteOwner_FulfillsOrderOnSecondRequest()
    {
        // create one order that will fail allocation the first time but succeed
        // when allocation is re-requested
        var orderIdOne = Guid.CreateVersion7();

        var skuOne = TestSkus.First();
        var lineOne = CreateLine(skuOne);

        var skuTwo = TestSkus.Skip(1).Take(1).First();
        var lineTwo = CreateLine(skuTwo, 99);

        var orderOne = CreateOrder(ShipCompleteOwner, orderIdOne, lineOne, lineTwo);

        var firstPassReceipts = new[]
        {
            CreateSkuReceipt(skuOne.Id, 1),
            CreateSkuReceipt(skuTwo.Id, 98)
        }.ToList();

        // create a second order that'll pass on the first attempts
        var orderIdTwo = Guid.CreateVersion7();
        var orderTwoSku = TestSkus.Skip(2).First();
        var orderTwoLine = CreateLine(orderTwoSku);
        var orderTwo = CreateOrder(ShipCompleteOwner, orderIdTwo, orderTwoLine);
        var orderTwoReceipt = CreateSkuReceipt(orderTwoSku.Id, 2);

        firstPassReceipts.Add(orderTwoReceipt);

        await SeedOrdersAndReceiptsAsync(firstPassReceipts, orderOne, orderTwo);

        var response = await PostOrderAllocationsAsync(
            new AllocateOrdersCommand([orderIdOne, orderIdTwo]),
            ShipCompleteOwner.Id);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await GetResult(response);
        Assert.That(result, Is.Not.Null);

        Assert.That(result.Orders.Single(s => s.OrderId == orderIdOne).Status,
            Is.EqualTo(AllocatedOrder.AllShortStatus));

        Assert.That(result.Orders.Single(s => s.OrderId == orderIdTwo).Status,
            Is.EqualTo(nameof(AllocatedOrder.FullyAllocatedStatus)));
    }

    private async Task SeedOrdersAndReceiptsAsync(IEnumerable<SkuReceipt> receipts, params OrderAllocation[] order)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await dbContext.SkuReceipts.AddRangeAsync(receipts);
        await dbContext.Orders.AddRangeAsync(order);
        await dbContext.SaveChangesAsync();
    }

    private sealed record AllocateOrdersResponse(
        bool Success,
        IReadOnlyCollection<AllocatedOrder> Orders,
        IReadOnlyCollection<AllocationError> Errors);

    #region Helpers

    private static OrderAllocation CreateOrder(Owner owner, Guid orderId, params OrderLineAllocation[] lines)
    {
        return OrderAllocation.Create(owner.Id, orderId, lines)
            .Value!;
    }

    private static OrderLineAllocation CreateLine(Sku sku, int quantityRequested = 1)
    {
        return OrderLineAllocation.Create(EntityId.Create(), sku.Id, quantityRequested)
            .Value!;
    }

    private static SkuReceipt CreateSkuReceipt(EntityId skudId, int quantityReceived)
    {
        return SkuReceipt.Create(EntityId.Create(), skudId, quantityReceived, AppDateTime.Now)
            .Value!;
    }

    private static Task<AllocationError[]?> GetBadRequestErrors(HttpResponseMessage message)
    {
        return message.Content.ReadFromJsonAsync<AllocationError[]>();
    }

    private static async Task<AllocateOrdersResponse?> GetResult(HttpResponseMessage response)
    {
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        return await response.Content.ReadFromJsonAsync<AllocateOrdersResponse>();
    }

    private async Task ResetAsync()
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Orders.ExecuteDeleteAsync();
        await dbContext.SkuReceipts.ExecuteDeleteAsync();
        // owners and skus are static lookup data, reused across tests
    }

    private async Task<HttpResponseMessage> PostOrderAllocationsAsync(AllocateOrdersCommand command, int ownerId)
    {
        var url = $"{InventoryExtensions.InventoryRoute}/orders/allocate";

        using var request = new HttpRequestMessage(
            HttpMethod.Post, url);
        request.Headers.Add("owner_id", ownerId.ToString());
        request.Content = JsonContent.Create(command);

        return await _client.SendAsync(request);
    }

    private async Task SeedTestData(OrderAllocation order, IEnumerable<SkuReceipt> receipts)
    {
    }

    #endregion
}