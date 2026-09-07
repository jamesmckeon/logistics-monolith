using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Throughline.Common.Results;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;
using Throughline.Modules.Ordering.Infrastructure.Orders;

namespace Throughline.Modules.Ordering.Tests.Application.CreateOrder;

[Category("Unit")]
public sealed class CreateOrderHandlerTests
{
    private OrdersDbContext _dbContext;
    private OrdersRepository _ordersRepository;
    private CreateOrderHandler _sut;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new OrdersDbContext(options);
        _ordersRepository = new OrdersRepository(_dbContext);
        _sut = new CreateOrderHandler(_ordersRepository, NullLogger<CreateOrderHandler>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task CreateOrderAsync_InvalidCommand_ReturnsFailure()
    {
        var command = new CreateOrderCommand(
            " ", "REF1", "123 Somewhere Drive", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var commandResult = command.Validate();

        var actual = await _sut.CreateOrderAsync(1, command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors, Is.EqualTo(commandResult.Errors));
            Assert.That(actual.ErrorType, Is.EqualTo(commandResult.ErrorType));
        });
    }

    [Test]
    public async Task CreateOrderAsync_InvalidPostalCode_ReturnsFailure()
    {
        var command = new CreateOrderCommand(
            "TESTPO", "REF1", "123 Somewhere Drive", null, "Portland", "OR",
            "$%^eRR", [new CreateOrderCommandItem("TestSku", 1)]);

        var postalCodeResult = PostalCode.Create(command.PostalCode);

        var actual = await _sut.CreateOrderAsync(1, command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors, Is.EqualTo(postalCodeResult.Errors));
            Assert.That(actual.ErrorType, Is.EqualTo(postalCodeResult.ErrorType));
        });
    }

    [Test]
    public async Task CreateOrderAsync_InvalidStreetAddress_ReturnsFailure()
    {
        var command = new CreateOrderCommand(
            "TESTPO", "REF1", "123 Somewhere Dr.", null, "Portland", "$$",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var addressResult = StreetAddress.Create(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            new PostalCode(command.PostalCode));

        var actual = await _sut.CreateOrderAsync(1, command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors, Is.EqualTo(addressResult.Errors));
            Assert.That(actual.ErrorType, Is.EqualTo(addressResult.ErrorType));
        });
    }

    [Test]
    public async Task CreateOrderAsync_InvalidContent_ReturnsFailure()
    {
        var command = new CreateOrderCommand(
            "PO1", "REF1", "Address One", null, "Portland", "OR",
            "97211", [
                new CreateOrderCommandItem("TestSku", 1),
                new CreateOrderCommandItem("TestSku", 1)
            ]);

        var address = new StreetAddress(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            new PostalCode(command.PostalCode));
        var lines = command.Items.Select(i => new OrderLine(new SkuCode(i.Sku), i.Quantity));
        var contentResult = OrderContent.Create(command.PurchaseOrderNumber, address, lines);

        var actual = await _sut.CreateOrderAsync(1, command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors, Is.EqualTo(contentResult.Errors));
            Assert.That(actual.ErrorType, Is.EqualTo(contentResult.ErrorType));
        });
    }

    [Test]
    public async Task CreateOrderAsync_OrderExistsWithDifferentContent_ReturnsConflict()
    {
        var command = new CreateOrderCommand(
            "PO1", "REF1", "Address One", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        // create an existing order with a different PO #
        var existingCommand = new CreateOrderCommand(
            "PO2", "REF1", "Address One", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var existing = TestOrder(existingCommand);
        _dbContext.Add(existing.ToOrderRecord());
        await _dbContext.SaveChangesAsync();

        var actual = await _sut.CreateOrderAsync(1, command);

        var expectedDescription =
            $"Reference #{command.ReferenceNumber} already identifies an order with different contents.";

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Conflict));
            Assert.That(actual.Errors.Single().Description, Is.EqualTo(expectedDescription));
        });
    }

    [Test]
    public async Task CreateOrderAsync_OrderExists_ReturnsFound()
    {
        var command = new CreateOrderCommand(
            "PO1", "REF1", "Address One", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var existing = TestOrder(command);
        _dbContext.Add(existing.ToOrderRecord());
        await _dbContext.SaveChangesAsync();

        var actual = await _sut.CreateOrderAsync(1, command);

        Assert.That(actual.Value, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value.Created, Is.False);
            Assert.That(actual.Value.OrderId, Is.EqualTo(existing.Id.Value));
            Assert.That(actual.Value.OwnerId,
                Is.EqualTo(existing.OwnerReferenceNumber.OwnerId));
            Assert.That(actual.Value.OwnerReferenceNumber,
                Is.EqualTo(existing.OwnerReferenceNumber.ReferenceNumber));
        });
    }

    [Test]
    public async Task CreateOrderAsync_OrderNotFound_SavesAndReturnsSuccess()
    {
        var command = new CreateOrderCommand(
            "PO1", "REF1", "Address One", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var actual = await _sut.CreateOrderAsync(1, command);

        Assert.That(actual.Value, Is.Not.Null);

        var order = await _dbContext.Orders.SingleAsync(s =>
            s.ReferenceNumber == command.ReferenceNumber);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value.Created, Is.True);
            Assert.That(actual.Value.OrderId, Is.EqualTo(order.OrderId));
            Assert.That(actual.Value.OwnerId, Is.EqualTo(order.OwnerId));
            Assert.That(actual.Value.OwnerReferenceNumber, Is.EqualTo(order.ReferenceNumber));
        });
    }

    #region Helpers

    private static CreateOrderCommand TestCommand()
    {
        return new CreateOrderCommand(
            "TESTPO", "REF1", "123 Somewhere Drive", null, "Portland", "$$",
            "@1f$4", [new CreateOrderCommandItem("TestSku", 1)]);
    }

    private static Order TestOrder(CreateOrderCommand command, int ownerId = 1)
    {
        var postalCode = new PostalCode(command.PostalCode);
        var streetAddress = new StreetAddress(
            command.StreetAddressOne, command.StreetAddressTwo, command.City, command.State, postalCode);
        var orderLines = command.Items.Select(i => new OrderLine(new SkuCode(i.Sku), i.Quantity))
            .ToList();

        return new Order(
            new OrderId(),
            new OwnerReferenceNumber(ownerId, command.ReferenceNumber),
            new OrderContent(command.PurchaseOrderNumber, streetAddress, orderLines));
    }

    #endregion
}