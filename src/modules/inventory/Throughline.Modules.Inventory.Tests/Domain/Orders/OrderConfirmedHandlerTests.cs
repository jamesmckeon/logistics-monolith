using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Orders;
using Throughline.Modules.Inventory.Infrastructure.Orders;
using Throughline.Modules.Ordering.Contracts.Events;
using Throughline.Modules.Ordering.Contracts.Models;

namespace Throughline.Modules.Inventory.Tests.Domain.Orders;

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
    public async Task Handle_NewOrder_SavesOrderAsNotAllocated()
    {
        var eventLine = new OrderLineEventModel("TESTSKU", 1);
        var message = new OrderConfirmedIntegrationEvent(1, Guid.NewGuid(), [eventLine]);

        var token = CancellationToken.None;
        var repository = new Mock<IOrderAllocationsRepository>();

        await _sut.Handle(
            message,
            repository.Object,
            NullLogger<OrderConfirmedHandler>.Instance,
            token);

        var id = new OwnerOrderId(message.OwnerId, message.OrderId);
        var expectedOrder = new Order(
            id,
            message.Lines.Select(l => new OrderLineAllocation(l.SkuCode, l.QuantityRequested)),
            OrderAllocationStatus.NotAllocated);


        repository.Verify(v => v.SaveConfirmedOrder(
            It.Is<Order>(o => o.Equals(expectedOrder)), token), Times.Once);
    }
}