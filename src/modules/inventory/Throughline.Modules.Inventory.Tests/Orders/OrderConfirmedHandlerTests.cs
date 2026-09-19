using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Throughline.Modules.Inventory.Orders;
using Throughline.Modules.Ordering.Contracts.Events;
using Throughline.Modules.Ordering.Contracts.Models;

namespace Throughline.Modules.Inventory.Tests.Orders;

[Category("Unit")]
internal sealed class OrderConfirmedHandlerTests
{
    private OrderConfirmedHandler _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new();
    }


    [Test]
    public async Task Handle_NewOrder_SavesOrder()
    {
        var eventLine = new OrderLineEventModel("TESTSKU", 1);
        var message = new OrderConfirmedIntegrationEvent(1, Guid.NewGuid(), [eventLine]);

        var token = CancellationToken.None;
        var repository = new Mock<IOrdersRepository>();

        await _sut.Handle(
            message,
            repository.Object,
            NullLogger<OrderConfirmedHandler>.Instance,
            token);

        var id = new OwnerOrderId(message.OwnerId, message.OrderId);
        var expectedOrder = new Order(id, message.Lines.Select(l => new OrderLine(l.SkuCode, l.QuantityRequested)));


        repository.Verify(v => v.SaveConfirmedOrder(
            It.Is<Order>(o => o.Equals(expectedOrder)), token), Times.Once);
    }
}