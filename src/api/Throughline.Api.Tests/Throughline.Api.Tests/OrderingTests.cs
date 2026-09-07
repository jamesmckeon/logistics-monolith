using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Application.Models;
using Throughline.Modules.Ordering.Infrastructure.Orders;
using Throughline.Modules.Ordering.Presentation;

namespace Throughline.Api.Tests;

[Category("Integration")]
public class OrderingTests
{
    private HttpClient _client;

    private TestFactory _factory;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestFactory();
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
        await _factory.ApplyMigrationsAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _factory.DisposeAsync();
        _client.Dispose();
    }

    [TearDown]
    public async Task TearDown()
    {
        await ResetAsync();
    }

    [Test]
    public async Task Post_InvalidRequest_ReturnsProblemDetails()
    {
        var command = new CreateOrderCommand(
            "  ",
            "testreference",
            "test address",
            null,
            "TestCity",
            "OR",
            "97211",
            [new CreateOrderCommandItem("TestSku", 1)]);

        var response = await PostOrder(command, 1);

        var problemDetails = await GetFromResponse(response);
        Assert.That(problemDetails, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problemDetails.Title, Is.EqualTo("One or more validation errors occurred"));
        });
    }

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

        var response = await PostOrder(newCommand, existingRecord.OwnerId);
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

        var response = await PostOrder(command, orderRecord.OwnerId);
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
    public async Task Post_NewOrder_ReturnsCreated()
    {
        var ownerId = 1;
        var command = TestCommand();

        var response = await PostOrder(command, ownerId);
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

    [Test]
    public async Task Post_ConcurrentEquivalentSubmissions_CreateExactlyOneRestAlreadyExist()
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

    #region Helpers

    // Fires all requests "at once": every task parks on the gate, then we release together.
    private async Task<IReadOnlyList<HttpResponseMessage>> PostConcurrently(
        IReadOnlyList<CreateOrderCommand> commands, int ownerId)
    {
        var gate = new TaskCompletionSource();
        var tasks = commands
            .Select(async cmd =>
            {
                await gate.Task; // park here until released
                return await PostOrder(cmd, ownerId); // reuses your existing helper
            })
            .ToArray();

        gate.SetResult(); // launch together
        return await Task.WhenAll(tasks);
    }

    private async Task<int> CountOrdersAsync(int ownerId, string reference)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        return await db.Orders.CountAsync(o => o.OwnerId == ownerId && o.ReferenceNumber == reference);
    }

    private static OrderRecord TestOrder(CreateOrderCommand command, int ownerId = 1)
    {
        var orderId = Guid.CreateVersion7();
        var orderRecord = new OrderRecord
        {
            OrderId = orderId,
            OwnerId = ownerId,
            PurchaseOrderNumber = command.PurchaseOrderNumber,
            ReferenceNumber = command.ReferenceNumber,
            StreetAddressOne = command.StreetAddressOne,
            StreetAddressTwo = command.StreetAddressTwo,
            City = command.City,
            State = command.State,
            Zipcode = command.PostalCode,
            OrderLines =
            [
                new OrderLineRecord
                {
                    OrderId = orderId,
                    SkuCode = "TestSku",
                    Quantity = 1
                }
            ]
        };
        return orderRecord;
    }

    private static CreateOrderCommand TestCommand()
    {
        return new CreateOrderCommand(
            "TESTPO",
            "testreference",
            "test address",
            null,
            "TestCity",
            "OR",
            "97211", [
                new CreateOrderCommandItem("TestSku", 1)
            ]);
    }

    private static CreateOrderCommand TestCommand(string purchaseOrderNumber)
    {
        return new CreateOrderCommand(
            purchaseOrderNumber,
            "testreference",
            "test address",
            null,
            "TestCity",
            "OR",
            "97211", [
                new CreateOrderCommandItem("TestSku", 1)
            ]);
    }

    private static async Task<ProblemDetails?> GetFromResponse(HttpResponseMessage response)
    {
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        var details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return details;
    }

    private async Task SeedAsync(Func<OrdersDbContext, Task> seed)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    private async Task ResetAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Orders.ExecuteDeleteAsync();
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