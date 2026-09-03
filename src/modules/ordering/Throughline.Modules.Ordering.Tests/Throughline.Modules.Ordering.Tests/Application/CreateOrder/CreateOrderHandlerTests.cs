using Microsoft.EntityFrameworkCore;
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
        _sut = new CreateOrderHandler(_ordersRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public void CreateOrderAsync_NullCommand_ThrowsExpected()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CreateOrderAsync(null!));
        Assert.That(ex.ParamName, Is.EqualTo("command"));
    }

    [Test]
    public async Task CreateOrderAsync_InvalidCommand_ReturnsFailure()
    {
        var command = new CreateOrderCommand(
            1, " ", "REF1", "123 Somewhere Drive", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var commandResult = command.Validate();

        var actual = await _sut.CreateOrderAsync(command);

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
            1, "TESTPO", "REF1", "123 Somewhere Drive", null, "Portland", "OR",
            "$%^eRR", [new CreateOrderCommandItem("TestSku", 1)]);

        var postalCodeResult = PostalCode.Create(command.PostalCode);

        var actual = await _sut.CreateOrderAsync(command);

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
            1, "TESTPO", "REF1", "123 Somewhere Dr.", null, "Portland", "$$",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var addressResult = StreetAddress.Create(
            command.StreetAddressOne,
            command.StreetAddressTwo,
            command.City,
            command.State,
            new PostalCode(command.PostalCode));

        var actual = await _sut.CreateOrderAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors, Is.EqualTo(addressResult.Errors));
            Assert.That(actual.ErrorType, Is.EqualTo(addressResult.ErrorType));
        });
    }

    [Test]
    public async Task CreateOrderAsync_OrderExists_ReturnsConflictFailure()
    {
        var command = new CreateOrderCommand(
            1, "PO1", "REF1", "Address One", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var existing = TestOrder(command);
        _dbContext.Add(existing.ToOrderRecord());
        await _dbContext.SaveChangesAsync();

        var expectedError =
            $"An order exists for owner #{command.OwnerId} with reference #{command.ReferenceNumber}";

        var actual = await _sut.CreateOrderAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors.Single().Description, Is.EqualTo(expectedError));
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Conflict));
        });
    }

    [Test]
    public async Task CreateOrderAsync_OrderNotFound_SavesAndReturnsSuccess()
    {
        var command = new CreateOrderCommand(
            1, "PO1", "REF1", "Address One", null, "Portland", "OR",
            "97211", [new CreateOrderCommandItem("TestSku", 1)]);

        var actual = await _sut.CreateOrderAsync(command);

        var order = actual.Value;
        Assert.That(order, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(order.ReferenceNumber, Is.EqualTo(command.ReferenceNumber));
            Assert.That(order.PurchaseOrderNumber, Is.EqualTo(command.PurchaseOrderNumber));
            Assert.That(order.OwnerId, Is.EqualTo(command.OwnerId));
        });
    }

    #region Helpers

    private static CreateOrderCommand TestCommand()
    {
        return new CreateOrderCommand(
            1, "TESTPO", "REF1", "123 Somewhere Drive", null, "Portland", "$$",
            "@1f$4", [new CreateOrderCommandItem("TestSku", 1)]);
    }

    private static Order TestOrder(CreateOrderCommand command)
    {
        var postalCode = new PostalCode(command.PostalCode);
        var streetAddress = new StreetAddress(
            command.StreetAddressOne, command.StreetAddressTwo, command.City, command.State, postalCode);
        var orderLines = command.Items.Select(i => new OrderLine(new SkuCode(i.Sku), i.Quantity))
            .ToList();

        return new Order(
            new OrderId(),
            command.OwnerId,
            command.PurchaseOrderNumber,
            command.ReferenceNumber,
            streetAddress,
            orderLines);
    }

    #endregion
}