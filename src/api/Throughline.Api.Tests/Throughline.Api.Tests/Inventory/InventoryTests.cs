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

        var skuTwo = TestSkus.Skip(1).First();
        var lineTwo = CreateLine(skuTwo, 99);

        var orderOne = CreateOrder(ShipCompleteOwner, orderIdOne, lineOne, lineTwo);

        var firstPassReceipts = new[]
        {
            CreateSkuReceipt(skuOne.Id, 2), // will be used by both orders
            CreateSkuReceipt(skuTwo.Id, 98)
        }.ToList();

        // create a second order that'll pass on the first attempts
        var orderIdTwo = Guid.CreateVersion7();
        var orderTwoLine = CreateLine(skuOne);
        var orderTwo = CreateOrder(ShipCompleteOwner, orderIdTwo, orderTwoLine);

        await SeedOrdersAndReceiptsAsync(firstPassReceipts, orderOne, orderTwo);

        var response = await PostOrderAllocationsAsync(
            new AllocateOrdersCommand([orderIdOne, orderIdTwo]),
            ShipCompleteOwner.Id);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await GetResult(response);
        Assert.That(result, Is.Not.Null);

        var orderOneFirstResult = result.Orders.Single(s => s.OrderId == orderIdOne);
        Assert.That(orderOneFirstResult.Status,
            Is.EqualTo(AllocatedOrder.FailedStatus));
        Assert.That(orderOneFirstResult.Errors.Single().Code,
            Is.EqualTo(AllocationError.PolicyNotSatisfiedCode));

        Assert.That(result.Orders.Single(s => s.OrderId == orderIdTwo).Status,
            Is.EqualTo(AllocatedOrder.FullyAllocatedStatus));

        // now add a receipt that will fulfill skuTwo on the first order
        await SeedReceiptsAsync(CreateSkuReceipt(skuTwo.Id, 1));

        var responseTwo = await PostOrderAllocationsAsync(
            new AllocateOrdersCommand([orderIdOne]),
            ShipCompleteOwner.Id);
        Assert.That(responseTwo.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var resultTwo = await GetResult(responseTwo);
        Assert.That(resultTwo, Is.Not.Null);

        var order = resultTwo.Orders.Single();

        Assert.Multiple(() =>
        {
            Assert.That(order.OrderId, Is.EqualTo(orderIdOne));
            Assert.That(order.Status, Is.EqualTo(AllocatedOrder.FullyAllocatedStatus));
        });
    }

    [Test]
    public async Task Post_MultipleReceiptsAllocated_UsesOldestFirst()
    {
        var orderIdOne = Guid.CreateVersion7();

        var skuOne = TestSkus.First();
        var lineOne = CreateLine(skuOne, 9);

        var orderOne = CreateOrder(ShipCompleteOwner, orderIdOne, lineOne);

        var now = AppDateTime.Now;
        var first = CreateSkuReceipt(skuOne.Id, 5, now.Subtract(new(0, 0, 5)));
        var second = CreateSkuReceipt(skuOne.Id, 4, now.Subtract(new(0, 0, 4)));
        var third = CreateSkuReceipt(skuOne.Id, 4, now);

        await SeedOrdersAndReceiptsAsync([first, second, third], orderOne);

        var response = await PostOrderAllocationsAsync(
            new AllocateOrdersCommand([orderIdOne]),
            ShipCompleteOwner.Id);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await GetResult(response);
        Assert.That(result, Is.Not.Null);

        var orderResult = result.Orders.Single();
        var receipts = await GetReceiptsAsync();

        Assert.That(orderResult.Status,
            Is.EqualTo(AllocatedOrder.FullyAllocatedStatus));
        Assert.That(orderResult.OrderId, Is.EqualTo(orderIdOne));
        Assert.That(receipts.Single(r => r.Id == first.Id).QuantityAllocated,
            Is.EqualTo(5));
        Assert.That(receipts.Single(r => r.Id == second.Id).QuantityAllocated,
            Is.EqualTo(4));
        Assert.That(receipts.Single(r => r.Id == third.Id).QuantityAllocated,
            Is.EqualTo(0));
    }

    [Test]
    public async Task Post_MultipleOrdersCompeteForStock_AllocatesInRequestedOrder()
    {
        var sku = TestSkus.First();

        // orderOne is created and seeded first, so it's the order the database is most
        // likely to return first; the request lists orderTwo first
        var orderIdOne = Guid.CreateVersion7();
        var orderOne = CreateOrder(PartialAllocationOwner, orderIdOne, CreateLine(sku));

        var orderIdTwo = Guid.CreateVersion7();
        var orderTwo = CreateOrder(PartialAllocationOwner, orderIdTwo, CreateLine(sku));

        // enough stock for only one of the orders
        await SeedOrdersAndReceiptsAsync([CreateSkuReceipt(sku.Id, 1)], orderOne, orderTwo);

        var response = await PostOrderAllocationsAsync(
            new AllocateOrdersCommand([orderIdTwo, orderIdOne]),
            PartialAllocationOwner.Id);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await GetResult(response);
        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Orders.Single(o => o.OrderId == orderIdTwo).Status,
                Is.EqualTo(AllocatedOrder.FullyAllocatedStatus));
            Assert.That(result.Orders.Single(o => o.OrderId == orderIdOne).Status,
                Is.EqualTo(AllocatedOrder.AllShortStatus));
        });
    }

    [Test]
    public async Task Post_OrderAlreadyFullyAllocated_CreatesNoAdditionalCommitments()
    {
        var orderId = Guid.CreateVersion7();
        var sku = TestSkus.First();
        var order = CreateOrder(PartialAllocationOwner, orderId, CreateLine(sku, 3));
        var receipt = CreateSkuReceipt(sku.Id, 5);

        await SeedOrdersAndReceiptsAsync([receipt], order);

        var command = new AllocateOrdersCommand([orderId]);

        var firstResponse = await PostOrderAllocationsAsync(command, PartialAllocationOwner.Id);
        Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var firstResult = await GetResult(firstResponse);
        Assert.That(firstResult, Is.Not.Null);
        Assert.That(firstResult.Orders.Single().Status, Is.EqualTo(AllocatedOrder.FullyAllocatedStatus));

        var secondResponse = await PostOrderAllocationsAsync(command, PartialAllocationOwner.Id);
        Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var secondResult = await GetResult(secondResponse);
        Assert.That(secondResult, Is.Not.Null);

        var receipts = await GetReceiptsAsync();
        var secondOrderResult = secondResult.Orders.Single();

        Assert.Multiple(() =>
        {
            Assert.That(secondOrderResult.OrderId, Is.EqualTo(orderId));
            Assert.That(secondOrderResult.Status, Is.EqualTo(AllocatedOrder.FullyAllocatedStatus));
            Assert.That(receipts.Single(r => r.Id == receipt.Id).QuantityAllocated, Is.EqualTo(3));
        });
    }


    #region Helpers

    private async Task SeedOrdersAndReceiptsAsync(IEnumerable<SkuReceipt> receipts, params OrderAllocation[] order)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await dbContext.SkuReceipts.AddRangeAsync(receipts);
        await dbContext.Orders.AddRangeAsync(order);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedReceiptsAsync(params SkuReceipt[] receipts)
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await dbContext.SkuReceipts.AddRangeAsync(receipts);
        await dbContext.SaveChangesAsync();
    }

    // reads through a fresh DbContext so the seeding context's tracked, pre-allocation
    // copies of the receipts aren't returned
    private async Task<List<SkuReceipt>> GetReceiptsAsync()
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        return await dbContext.SkuReceipts.ToListAsync();
    }

    private sealed record AllocateOrdersResponse(
        bool Success,
        IReadOnlyCollection<AllocatedOrder> Orders,
        IReadOnlyCollection<AllocationError> Errors);


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

    private static SkuReceipt CreateSkuReceipt(EntityId skudId, int quantityReceived, AppDateTime receivedOn)
    {
        return SkuReceipt.Create(EntityId.Create(), skudId, quantityReceived, receivedOn)
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

    #endregion
}