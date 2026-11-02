using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Throughline.Modules.Inventory.Application.AllocateOrders;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Owners;

namespace Throughline.Modules.Inventory.Tests.Application.AllocateOrders;

[Category("Unit")]
internal sealed class AllocateOrdersHandlerTests
{
    private Mock<IOrderAllocationRepository> _orderAllocationRepository;
    private Mock<IOrderAllocationService> _orderAllocationService;
    private Mock<IOwnerProvider> _ownerProvider;
    private AllocateOrdersHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _orderAllocationRepository = new Mock<IOrderAllocationRepository>(MockBehavior.Strict);
        _orderAllocationService = new Mock<IOrderAllocationService>(MockBehavior.Strict);
        _ownerProvider = new Mock<IOwnerProvider>(MockBehavior.Strict);

        _sut = new AllocateOrdersHandler(
            NullLogger<AllocateOrdersHandler>.Instance,
            _orderAllocationService.Object,
            _orderAllocationRepository.Object,
            _ownerProvider.Object);
    }

    [Test]
    public async Task AllocateOrdersAsync_CapExeeded_ReturnsFailure()
    {
        var orders = Enumerable.Range(1, 51).Select(_ => Guid.NewGuid())
            .ToList();
        var command = new AllocateOrdersCommand(orders);

        var result = await _sut.AllocateOrdersAsync(1, command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsBadRequest, Is.True);
            Assert.That(result.Errors.Single().Code,
                Is.EqualTo(AllocationError.OrderCapExceededCode));
            Assert.That(result.Errors.Single().Description,
                Is.EqualTo("Only 50 order ids can be allocated at once"));
        });
    }
}