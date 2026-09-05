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
    public async Task Post_OrderExists_ReturnsConflict()
    {
        var command = TestCommand();
        var orderRecord = TestOrder(command);

        await SeedAsync(db =>
        {
            db.Orders.Add(orderRecord);
            return Task.CompletedTask;
        });

        var expectedMessage =
            $"An order exists for owner #{orderRecord.OwnerId} with reference #{command.ReferenceNumber}";

        var response = await PostOrder(command, orderRecord.OwnerId);

        var problemDetails = await GetFromResponse(response);
        Assert.That(problemDetails, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(problemDetails.Title, Is.EqualTo("A conflict occurred"));
            Assert.That(problemDetails.Detail, Is.EqualTo(expectedMessage));
        });
    }


    [Test]
    public async Task Post_NewOrder_ReturnsCreatedWithModel()
    {
        var command = TestCommand();

        var expectedAddress = new DestinationModel(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            command.PostalCode);

        IEnumerable<OrderLineModel> expectedLines =
            [new(command.Items.Single().Sku.ToUpper(), command.Items.Single().Quantity)];

        var response = await PostOrder(command, 1);
        var model = await response.Content.ReadFromJsonAsync<OrderModel>();

        Assert.That(model, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(model.PurchaseOrderNumber, Is.EqualTo(command.PurchaseOrderNumber));
            Assert.That(model.OwnerId, Is.EqualTo(1));
            Assert.That(model.ReferenceNumber, Is.EqualTo(command.ReferenceNumber));
            Assert.That(model.Destination, Is.EqualTo(expectedAddress));
            Assert.That(model.OrderLines, Is.EquivalentTo(expectedLines));
            Assert.That(response.Headers.Location?.ToString(), Is.EqualTo($"/orders/{model.OrderId}"));
        });
    }


    [Test]
    public async Task Get_OrderExists_ReturnsModel()
    {
        var command = TestCommand();
        var orderRecord = TestOrder(command);

        await SeedAsync(db =>
        {
            db.Orders.Add(orderRecord);
            return Task.CompletedTask;
        });

        var expectedAddress = new DestinationModel(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            command.PostalCode);

        var expectedLines =
            orderRecord.OrderLines.Select(l => new OrderLineModel(l.SkuCode, l.Quantity));

        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"{OrderingExtensions.OrdersRoute}/{orderRecord.OrderId}");
        request.Headers.Add("owner_id", orderRecord.OwnerId.ToString());

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var model = await response.Content.ReadFromJsonAsync<OrderModel>();

        Assert.That(model, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(model.PurchaseOrderNumber, Is.EqualTo(command.PurchaseOrderNumber));
            Assert.That(model.OwnerId, Is.EqualTo(orderRecord.OwnerId));
            Assert.That(model.ReferenceNumber, Is.EqualTo(command.ReferenceNumber));
            Assert.That(model.Destination, Is.EqualTo(expectedAddress));
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

    #region Helpers

    private static OrderRecord TestOrder(CreateOrderCommand command, int ownerId = 1)
    {
        var orderRecord = new OrderRecord
        {
            OrderId = Guid.NewGuid(),
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
                    OrderId = Guid.NewGuid(),
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