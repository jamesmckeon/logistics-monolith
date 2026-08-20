using Throughline.Common.Results;
using Throughline.Modules.Ordering.Application.CreateOrder;
using Throughline.Modules.Ordering.Application.Orders;
using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Orders;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Application.Tests.Orders;

[Category("Unit")]
public sealed class CreateOrderHandlerTests
{
    private Mock<IOrderEstimateService> _orderEstimateService;
    private Mock<IOrdersRepository> _ordersRepository;
    private Mock<IOrderEstimateRequestBuilder> _requestBuilder;
    private CreateOrderHandler _sut;

    [SetUp]
    public void Setup()
    {
        _ordersRepository = new();
        _requestBuilder = new();
        _orderEstimateService = new();
        _sut = new(
            _requestBuilder.Object,
            _orderEstimateService.Object,
            _ordersRepository.Object);
    }

    [Test]
    public void CreateOrderAsync_NullCommand_ThrowsExpected()
    {
        var ex = Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CreateOrderAsync(null!));
        Assert.That(ex.ParamName, Is.EqualTo("command"));
    }

    [Test]
    public async Task CreateOrderAsync_InvalidAddress_ReturnsFailure()
    {
        var command = CreateOrderCommand.Create(
                1, "PO1", "123 Somewhere Drive", null, "Portland", "$$", "@1f$4",
                [("TestSku", 1)], "REF1")
            .Value!;

        SetupOrderExists(command);

        var actual = await _sut.CreateOrderAsync(command);

        var stateResult = AddressState.Create(command.State);
        var codeResult = PostalCode.Create(command.PostalCode);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors.Count(), Is.EqualTo(2));
            Assert.That(actual.Errors.Any(a =>
                a == actual.Errors.First()));
            Assert.That(actual.Errors.Any(a =>
                a == actual.Errors.Last()));
        });
    }

    [Test]
    public async Task CreateOrderAsync_OrderExists_ReturnsFailure()
    {
        var command = TestCommand();

        SetupOrderExists(command, true);

        var actual = await _sut.CreateOrderAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors.Single().ErrorType,
                Is.EqualTo(ErrorType.Conflict));
            Assert.That(actual.Errors.Single().Description,
                Is.EqualTo(
                    $"An order exists for merchant #{command.MerchantId} with reference #{command.ReferenceNumber}"));
        });
    }

    [Test]
    public async Task CreateOrderAsync_BuilderError_ReturnsFailure()
    {
        var command = TestCommand();
        SetupOrderExists(command);

        var error = Error.Validation("Test Error");
        _requestBuilder.Setup(s => s.CreateRequestAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrderEstimateRequest>.Failure(error));

        var actual = await _sut.CreateOrderAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.Errors.Single(), Is.EqualTo(error));
        });
    }

    [Test]
    public async Task CreateOrderAsync_EstimateCreated_SavesOrder()
    {
        var command = TestCommand();
        SetupOrderExists(command);

        var requestItems = new OrderEstimateRequestItem[]
        {
            new(new("TESTSKU"), 1, 1.23m, new(2.34m))
        };

        var request = new OrderEstimateRequest(
            new Rate(9.87m),
            requestItems,
            new ZoneSurcharge(1,
                new PostalZone(new("12345"), new("23456")),
                new Money(99.99m)));

        var estimateItems = new OrderEstimateItem[]
        {
            new(new("TESTSKU"), 1, new(1.23m), new(2.34m), 3.45m, new(4.56m))
        };

        var estimate = new OrderEstimate(
            new(1), new(2.12m), new(3.45m), estimateItems);

        _requestBuilder.Setup(s => s.CreateRequestAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _orderEstimateService.Setup(s => s.GetEstimate(request))
            .Returns(estimate);

        Order savedOrder = null!;
        _ordersRepository.Setup(s => s.SaveOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => savedOrder = o)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateOrderAsync(command);

        var expected = OrderModel.FromOrder(savedOrder);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);

            var order = result.Value!;
            Assert.That(order.OrderId, Is.EqualTo(expected.OrderId));
            Assert.That(order.MerchantId, Is.EqualTo(expected.MerchantId));
            Assert.That(order.PurchaseOrderNumber, Is.EqualTo(expected.PurchaseOrderNumber));
            Assert.That(order.ReferenceNumber, Is.EqualTo(expected.ReferenceNumber));
            Assert.That(order.Destination, Is.EqualTo(expected.Destination));
            Assert.That(order.DestinationSurcharge, Is.EqualTo(expected.DestinationSurcharge));
            Assert.That(order.TotalCharges, Is.EqualTo(expected.TotalCharges));
            Assert.That(order.OrderLines, Is.EqualTo(expected.OrderLines));
        });

        _ordersRepository.Verify(s => s.SaveOrderAsync(savedOrder, It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Helpers

    private static CreateOrderCommand TestCommand()
    {
        return CreateOrderCommand.Create(
            1, "PO1", "123 Somewhere Drive", null, "Portland", "OR", "97218",
            [("TestSku", 1)], "REF1").Value!;
    }

    private void SetupOrderExists(CreateOrderCommand command, bool exists = false)
    {
        _ordersRepository.Setup(s => s.OrderExistsFor(
                command.MerchantId, command.ReferenceNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
    }

    #endregion
}