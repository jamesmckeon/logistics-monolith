using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Orders;
using Throughline.Modules.Inventory.Infrastructure.Common;
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
        var repository = new Mock<IOrderAllocationRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        await _sut.Handle(
            message,
            repository.Object,
            NullLogger<OrderConfirmedHandler>.Instance,
            unitOfWork.Object,
            token);

        var expectedOrder = new OrderAllocation(
            message.OwnerId,
            message.OrderId,
            message.Lines.Select(l => new OrderLineAllocation(l.SkuCode, l.QuantityRequested)),
            OrderAllocationStatus.NotAllocated);


        repository.Verify(v => v.Add(
                It.Is<OrderAllocation>(o => o.Equals(expectedOrder))),
            Times.Once);

        unitOfWork.Verify(v => v.SaveChangesAsync(), Times.Once);
    }
}