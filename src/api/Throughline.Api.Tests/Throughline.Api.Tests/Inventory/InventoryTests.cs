using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Api.Tests.Common;
using Throughline.Common.Models;
using Throughline.Common.Presentation.Auth;
using Throughline.Modules.Inventory.Application.AllocateOrders;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Db;
using Throughline.Modules.Inventory.Presentation;
using Throughline.Modules.Ordering.Contracts.Events;
using Throughline.Modules.Ordering.Contracts.Models;
using Throughline.Modules.Receiving.Contracts.Events;
using Throughline.Modules.Receiving.Contracts.Models;
using Wolverine.Tracking;

namespace Throughline.Api.Tests.Inventory;

[Category("Integration")]
internal class InventoryTests : IntegrationTestsBase<InventoryDbContext>
{
    private List<Sku> TestSkus { get; set; }
    private Owner PartialAllocationOwner { get; set; }
    private Owner ShipCompleteOwner { get; set; }

    [SetUp]
    public async Task SetUp()
    {
        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        TestSkus = Enumerable.Range(1, 5).Select(i => new Sku(EntityId.Create(), 1, $"SKU{i}"))
            .ToList();
        await dbContext.Skus.AddRangeAsync(TestSkus);

        PartialAllocationOwner = new Owner(1, "Partial Allocation Owner", AllocationPolicies.Partial);
        dbContext.Owners.Add(PartialAllocationOwner);
        ShipCompleteOwner = new Owner(2, "Ship Complete Owner", AllocationPolicies.ShipComplete);
        dbContext.Owners.Add(ShipCompleteOwner);

        await dbContext.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await ResetAsync();
    }

    #region OrderConfirmedIntegrationEvent

    [Test]
    public async Task OrderConfirmedEvent_ZeroQuantityRequested_ThrowsExpected()
    {
        var sku = new Sku(EntityId.Create(), 1, "TESTSKU");

        await using var scope = _testFactory.Services.CreateAsyncScope();
        var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await inventoryDbContext.Skus.AddAsync(sku);
        await inventoryDbContext.SaveChangesAsync();

        // trigger a OrderLineAllocation.create() failure
        var orderId = Guid.NewGuid();
        var orderLine = new OrderLineEventModel(sku.Code, 0);
        var orderAllocatedEvent = new OrderConfirmedIntegrationEvent(1, orderId, AppDateTime.Now.Value, [orderLine]);

        var session = await _testFactory.Services.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .SendMessageAndWaitAsync(orderAllocatedEvent);

        var message = session
            .MovedToErrorQueue
            .SingleMessage<OrderConfirmedIntegrationEvent>();

        Assert.That(message, Is.Not.Null);
        Assert.That(message.OrderId, Is.EqualTo(orderId));
    }

    [Test]
    public async Task OrderConfirmedEvent_InvalidSku_RequeuesMessage()
    {
        // dont setup any skus in Inventory
        var orderId = Guid.NewGuid();
        var orderConfirmedEvent = new OrderConfirmedIntegrationEvent(
            1, orderId, AppDateTime.Now.Value, [new OrderLineEventModel("TEST", 1)]);

        var session = await _testFactory.Services.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .DoNotAssertOnTimeout()
            .Timeout(TimeSpan.FromSeconds(3))
            .SendMessageAndWaitAsync(orderConfirmedEvent);

        // var exception = session.AllExceptions().Single();
        var requeuedEvent = session.Requeued.SingleMessage<OrderConfirmedIntegrationEvent>();

        Assert.Multiple(() =>
        {
            Assert.That(requeuedEvent.OrderId, Is.EqualTo(orderId));
            Assert.That(session.MovedToErrorQueue.AllMessages, Is.Empty);
        });
    }

    #endregion

    #region AllocatablePalletsIntegrationEvent

    [Test]
    public async Task AllocatablePalletsEvent_ZeroQuantityReceived_ThrowsExpected()
    {
        var sku = new Sku(EntityId.Create(), 1, "TESTSKU");

        await using var scope = _testFactory.Services.CreateAsyncScope();
        var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await inventoryDbContext.Skus.AddAsync(sku);
        await inventoryDbContext.SaveChangesAsync();

        // trigger a skureceipt.create() failure
        var receiptId = Guid.NewGuid();
        var palletsEvent = new AllocatablePalletsIntegrationEvent(
            1, receiptId, "R-TEST", DateTimeOffset.UtcNow.AddMinutes(-1),
            [new AllocatablePalletModel("TESTLPN", sku.Code, 0, "BULK-01")]);

        var session = await _testFactory.Services.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .SendMessageAndWaitAsync(palletsEvent);

        var message = session
            .MovedToErrorQueue
            .SingleMessage<AllocatablePalletsIntegrationEvent>();

        Assert.That(message, Is.Not.Null);
        Assert.That(message.ReceiptId, Is.EqualTo(receiptId));
    }

    [Test]
    public async Task AllocatablePalletsEvent_InvalidSku_RequeuesMessage()
    {
        // dont setup any skus in Inventory
        var receiptId = Guid.NewGuid();
        var palletsEvent = new AllocatablePalletsIntegrationEvent(
            1, receiptId, "R-TEST", DateTimeOffset.UtcNow.AddMinutes(-1),
            [new AllocatablePalletModel("TESTLPN", "TESTSKU", 0, "BULK-01")]);

        var session = await _testFactory.Services.TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .DoNotAssertOnTimeout()
            .Timeout(TimeSpan.FromSeconds(3))
            .SendMessageAndWaitAsync(palletsEvent);

        // var exception = session.AllExceptions().Single();
        var requeuedEvent = session.Requeued.SingleMessage<AllocatablePalletsIntegrationEvent>();

        Assert.Multiple(() =>
        {
            Assert.That(requeuedEvent.ReceiptId, Is.EqualTo(receiptId));
            Assert.That(session.MovedToErrorQueue.AllMessages, Is.Empty);
        });
    }

    #endregion

    #region Allocate

    [Test]
    public async Task Allocate_NotInOutboundPlanningRole_ReturnsForbidden()
    {
        var command = new AllocateOrdersCommand([Guid.NewGuid()]);

        using var request = CreateRequest(
            HttpMethod.Post, AllocationRoute, 1, ThroughlineRoles.OwnerRole(1));
        request.Content = JsonContent.Create(command);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Post_NotInOwnerRole_ReturnsForbidden()
    {
        var command = new AllocateOrdersCommand([Guid.NewGuid()]);

        using var request = CreateRequest(
            HttpMethod.Post, AllocationRoute, 1, ThroughlineRoles.OutboundPlanning);
        request.Content = JsonContent.Create(command);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
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
        order.StartAllocating(AppDateTime.Now);

        await using var scope = _testFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var command = new AllocateOrdersCommand([orderId]);
        var response = await PostOrderAllocationsAsync(command, PartialAllocationOwner.Id);

        var result = await GetResult(response);
        Assert.That(result, Is.Not.Null);

        var resultOrder = result.Orders.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(resultOrder.OrderId, Is.EqualTo(orderId));
            Assert.That(resultOrder.Errors.Single().Code, Is.EqualTo(AllocationError.OrderAllocatingCode));
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

    #endregion

    #region Worklists

    // the policy is attached to each route, so each is checked
    [TestCase(UnreleasedList)]
    [TestCase(BackorderedList)]
    public async Task GetWorklist_NotInOutboundPlanningRole_ReturnsForbidden(string list)
    {
        using var request = CreateRequest(
            HttpMethod.Get, WorklistRoute(list, null), 1, ThroughlineRoles.OwnerRole(1),
            ThroughlineRoles.Inventory);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetWorklists_OneOrderReleasedShort_EachListHasOnlyItsOwnOrder()
    {
        var sku = TestSkus.First();
        var released = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), CreateLine(sku));
        var unreleased = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), CreateLine(sku));
        await SeedOrdersAndReceiptsAsync([], released, unreleased);

        // no stock, so the released order comes back short
        var allocateResponse = await PostOrderAllocationsAsync(
            new AllocateOrdersCommand([released.Id]), PartialAllocationOwner.Id);
        Assert.That(allocateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var unreleasedResponse = await GetWorklistAsync(UnreleasedList, PartialAllocationOwner.Id, null);
        var backorderedResponse = await GetWorklistAsync(BackorderedList, PartialAllocationOwner.Id, null);
        var unreleasedOrders = await unreleasedResponse.Content.ReadFromJsonAsync<List<OrderAllocationModel>>();
        var backorderedOrders = await backorderedResponse.Content.ReadFromJsonAsync<List<OrderAllocationModel>>();

        Assert.Multiple(() =>
        {
            Assert.That(unreleasedOrders?.Select(o => o.OrderId), Is.EqualTo([unreleased.Id]));
            Assert.That(backorderedOrders?.Select(o => o.OrderId), Is.EqualTo([released.Id]));
        });
    }

    [Test]
    public async Task GetUnreleased_MixedOrders_ReturnsOnlyOwnersUnallocatedIdleOrders()
    {
        var sku = TestSkus.First();
        var awaiting = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), CreateLine(sku));

        var allocatedLine = CreateLine(sku);
        var receipt = CreateSkuReceipt(sku.Id, 1);
        allocatedLine.AllocateReceipt(receipt, AppDateTime.Now);
        var allocated = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), allocatedLine);

        var allocating = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), CreateLine(sku));
        allocating.StartAllocating(AppDateTime.Now);

        var otherOwners = CreateOrder(ShipCompleteOwner, Guid.CreateVersion7(), CreateLine(sku));

        await SeedOrdersAndReceiptsAsync([receipt], awaiting, allocated, allocating, otherOwners);

        var response = await GetWorklistAsync(UnreleasedList, PartialAllocationOwner.Id, null);
        var worklist = await response.Content.ReadFromJsonAsync<List<OrderAllocationModel>>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(worklist?.Select(o => o.OrderId), Is.EqualTo([awaiting.Id]));
        });
    }

    [Test]
    public async Task GetUnreleased_BatchSize_ReturnsEarliestCreatedThenOrderId()
    {
        var sku = TestSkus.First();
        var early = new NonFutureDateTime(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero));
        var tied = new NonFutureDateTime(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero));
        var late = new NonFutureDateTime(new DateTimeOffset(2026, 10, 1, 11, 0, 0, TimeSpan.Zero));

        var lateOrder = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), late, CreateLine(sku));
        var tiedOne = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), tied, CreateLine(sku));
        var earlyOrder = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), early, CreateLine(sku));
        var tiedTwo = CreateOrder(PartialAllocationOwner, Guid.CreateVersion7(), tied, CreateLine(sku));

        await SeedOrdersAndReceiptsAsync([], lateOrder, tiedOne, earlyOrder, tiedTwo);

        // Postgres orders uuids by their text form
        var tiedInIdOrder = new[] { tiedOne.Id, tiedTwo.Id }
            .OrderBy(id => id.ToString(), StringComparer.Ordinal);
        var expected = new[] { earlyOrder.Id }.Concat(tiedInIdOrder);

        var response = await GetWorklistAsync(UnreleasedList, PartialAllocationOwner.Id, 3);
        var worklist = await response.Content.ReadFromJsonAsync<List<OrderAllocationModel>>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(worklist?.Select(o => o.OrderId), Is.EqualTo(expected));
        });
    }

    #endregion

    #region Helpers

    private static OrderAllocation CreateOrder(
        Owner owner, Guid orderId, NonFutureDateTime createdOn, params OrderLineAllocation[] lines) =>
        OrderAllocation.Create(owner.Id, orderId, createdOn, lines).Value
        ?? throw new InvalidOperationException("test order could not be created");

    private const string UnreleasedList = "unreleased";
    private const string BackorderedList = "backordered";

    private async Task<HttpResponseMessage> GetWorklistAsync(string list, int ownerId, int? batchSize)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            WorklistRoute(list, batchSize),
            ownerId,
            ThroughlineRoles.OwnerRole(ownerId),
            ThroughlineRoles.OutboundPlanning);

        return await _client.SendAsync(request);
    }

    private static string WorklistRoute(string list, int? batchSize) =>
        $"{InventoryExtensions.InventoryRoute}/orders/{list}" +
        (batchSize is null ? "" : $"?batchSize={batchSize}");

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


    private static OrderAllocation CreateOrder(Owner owner, Guid orderId, params OrderLineAllocation[] lines) =>
        OrderAllocation.Create(owner.Id, orderId, AppDateTime.Now, lines)
            .Value!;

    private static OrderLineAllocation CreateLine(Sku sku, int quantityRequested = 1) =>
        OrderLineAllocation.Create(EntityId.Create(), sku.Id, quantityRequested)
            .Value!;

    private static SkuReceipt CreateSkuReceipt(EntityId skudId, int quantityReceived) =>
        SkuReceipt.Create(EntityId.Create(), skudId, quantityReceived, AppDateTime.Now)
            .Value!;

    private static SkuReceipt CreateSkuReceipt(EntityId skudId, int quantityReceived, AppDateTime receivedOn) =>
        SkuReceipt.Create(EntityId.Create(), skudId, quantityReceived, receivedOn)
            .Value!;

    private static Task<AllocationError[]?> GetBadRequestErrors(HttpResponseMessage message) =>
        message.Content.ReadFromJsonAsync<AllocationError[]>();

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
        await dbContext.Owners.ExecuteDeleteAsync();
        await dbContext.Skus.ExecuteDeleteAsync();
    }

    private async Task<HttpResponseMessage> PostOrderAllocationsAsync(AllocateOrdersCommand command, int ownerId)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            AllocationRoute,
            ownerId,
            ThroughlineRoles.OwnerRole(ownerId),
            ThroughlineRoles.OutboundPlanning);

        request.Content = JsonContent.Create(command);

        return await _client.SendAsync(request);
    }

    private static string AllocationRoute => $"{InventoryExtensions.InventoryRoute}/orders/allocate";

    internal static string GetAllocatedOrderRoute(Guid orderId) =>
        $"{InventoryExtensions.InventoryRoute}/orders/{orderId}";

    #endregion
}