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
            1,
            "  ",
            "testreference",
            "test address",
            null,
            "TestCity",
            "OR",
            "97211",
            [new CreateOrderCommandItem("TestSku", 1)]);

        var response = await _client.PostAsJsonAsync(OrderingExtensions.OrdersRoute, command);

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

        var orderRecord = new OrderRecord
        {
            OrderId = Guid.NewGuid(),
            MerchantId = command.MerchantId,
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

        await SeedAsync(db =>
        {
            db.Orders.Add(orderRecord);
            return Task.CompletedTask;
        });

        var expectedMessage =
            $"An order exists for merchant #{command.MerchantId} with reference #{command.ReferenceNumber}";

        var response = await _client.PostAsJsonAsync(OrderingExtensions.OrdersRoute, command);

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
    public async Task Post_NewOrder_ReturnsOkWithModel()
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

        var response = await _client.PostAsJsonAsync(OrderingExtensions.OrdersRoute, command);
        response.EnsureSuccessStatusCode();
        var model = await response.Content.ReadFromJsonAsync<OrderModel>();

        Assert.That(model, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(model.PurchaseOrderNumber, Is.EqualTo(command.PurchaseOrderNumber));
            Assert.That(model.MerchantId, Is.EqualTo(command.MerchantId));
            Assert.That(model.ReferenceNumber, Is.EqualTo(command.ReferenceNumber));
            Assert.That(model.Destination, Is.EqualTo(expectedAddress));
            Assert.That(model.OrderLines, Is.EquivalentTo(expectedLines));
        });
    }


    #region Helpers

    private static CreateOrderCommand TestCommand()
    {
        return new CreateOrderCommand(
            1,
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

    #endregion
}