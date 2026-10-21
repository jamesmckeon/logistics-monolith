using Moq;
using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Application.Queries;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;

namespace Throughline.Modules.Inventory.Tests.Application.Queries;

[Category("Unit")]
internal sealed class GetAllocationWorklistQueryTests
{
    private const int OwnerId = 1;

    private static readonly CancellationTokenSource TokenSource = new();
    private static readonly Sku SkuOne = new(EntityId.Create(), OwnerId, "SKU1");
    private static readonly Sku SkuTwo = new(EntityId.Create(), OwnerId, "SKU2");

    private Mock<IInventoryRepository> _inventoryRepository;
    private Mock<IOrderAllocationRepository> _orderAllocationRepository;
    private GetAllocationWorklistQuery _sut;
    private static CancellationToken Token => TokenSource.Token;

    [SetUp]
    public void SetUp()
    {
        _inventoryRepository = new Mock<IInventoryRepository>(MockBehavior.Strict);
        _orderAllocationRepository = new Mock<IOrderAllocationRepository>(MockBehavior.Strict);

        _sut = new GetAllocationWorklistQuery(_orderAllocationRepository.Object, _inventoryRepository.Object);
    }

    #region GetWorklistAsync

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(501)]
    public async Task GetWorklistAsync_BatchSizeOutOfRange_ReturnsValidationFailure(int batchSize)
    {
        var actual = await _sut.GetWorklistAsync(OwnerId, AllocationWorklist.Unreleased, batchSize, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
        });
    }

    [Test]
    public async Task GetWorklistAsync_NoBatchSize_RequestsBatchOf25()
    {
        var order = CreateOrder(AppDateTime.Now, CreateLine(SkuOne));
        GivenUnreleased(25, order);
        GivenSkus(SkuOne);

        var actual = await _sut.GetWorklistAsync(OwnerId, AllocationWorklist.Unreleased, null, Token);

        Assert.That(actual.Value?.Select(o => o.OrderId), Is.EqualTo([order.Id]));
    }

    [TestCase(1)]
    [TestCase(500)]
    public async Task GetWorklistAsync_BatchSizeInRange_RequestsThatBatchSize(int batchSize)
    {
        var order = CreateOrder(AppDateTime.Now, CreateLine(SkuOne));
        GivenUnreleased(batchSize, order);
        GivenSkus(SkuOne);

        var actual = await _sut.GetWorklistAsync(OwnerId, AllocationWorklist.Unreleased, batchSize, Token);

        Assert.That(actual.Value?.Select(o => o.OrderId), Is.EqualTo([order.Id]));
    }

    [Test]
    public async Task GetWorklistAsync_NothingAwaiting_ReturnsEmptyOrders()
    {
        GivenUnreleased(25);
        GivenSkus();

        var actual = await _sut.GetWorklistAsync(OwnerId, AllocationWorklist.Unreleased, null, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value, Is.Empty);
        });
    }

    [Test]
    public async Task GetWorklistAsync_OrdersAwaiting_KeepsRepositoryOrder()
    {
        var first = CreateOrder(AppDateTime.Now, CreateLine(SkuOne));
        var second = CreateOrder(AppDateTime.Now, CreateLine(SkuTwo));
        GivenUnreleased(25, first, second);
        GivenSkus(SkuOne, SkuTwo);

        var actual = await _sut.GetWorklistAsync(OwnerId, AllocationWorklist.Unreleased, null, Token);

        Assert.That(actual.Value?.Select(o => o.OrderId), Is.EqualTo([first.Id, second.Id]));
    }

    [Test]
    public async Task GetWorklistAsync_OrderAwaiting_MapsOrderFields()
    {
        var createdOn = new NonFutureDateTime(new DateTimeOffset(2026, 10, 1, 14, 30, 0, TimeSpan.Zero));
        var order = CreateOrder(createdOn, CreateLine(SkuOne));
        GivenUnreleased(25, order);
        GivenSkus(SkuOne);

        var actual = await _sut.GetWorklistAsync(OwnerId, AllocationWorklist.Unreleased, null, Token);

        var model = actual.Value?.Single();

        Assert.Multiple(() =>
        {
            Assert.That(model?.OwnerId, Is.EqualTo(OwnerId));
            Assert.That(model?.OrderId, Is.EqualTo(order.Id));
            Assert.That(model?.OrderCreatedOn, Is.EqualTo(createdOn.Value));
            Assert.That(model?.AllocationStatus, Is.EqualTo(nameof(AllocationStatuses.Confirmed)));
        });
    }

    [Test]
    public async Task GetWorklistAsync_LinesPartlyAllocated_MapsSkuCodeAndQuantityToAllocate()
    {
        var partialLine = CreateLine(SkuOne, 3);
        Allocate(partialLine, 1);
        var allocatedLine = CreateLine(SkuTwo);
        Allocate(allocatedLine, 1);
        var order = CreateOrder(AppDateTime.Now, partialLine, allocatedLine);
        GivenUnreleased(25, order);
        GivenSkus(SkuOne, SkuTwo);

        var expected = new[]
        {
            new OrderLineAllocationModel(SkuOne.Code, 3, 1, 2, nameof(AllocationStatuses.PartiallyAllocated)),
            new OrderLineAllocationModel(SkuTwo.Code, 1, 1, 0, nameof(AllocationStatuses.Allocated))
        };

        var actual = await _sut.GetWorklistAsync(OwnerId, AllocationWorklist.Unreleased, null, Token);

        Assert.That(actual.Value?.Single().Lines, Is.EquivalentTo(expected));
    }

    [TestCase(AllocationWorklist.Unreleased)]
    [TestCase(AllocationWorklist.Backordered)]
    public async Task GetWorklistAsync_Worklist_RequestsThatWorklist(AllocationWorklist worklist)
    {
        var order = CreateOrder(AppDateTime.Now, CreateLine(SkuOne));
        GivenWorklist(worklist, 25, order);
        GivenSkus(SkuOne);

        var actual = await _sut.GetWorklistAsync(OwnerId, worklist, null, Token);

        Assert.That(actual.Value?.Select(o => o.OrderId), Is.EqualTo([order.Id]));
    }

    #endregion

    #region Helpers

    private void GivenUnreleased(int batchSize, params OrderAllocation[] orders)
    {
        GivenWorklist(AllocationWorklist.Unreleased, batchSize, orders);
    }

    private void GivenWorklist(AllocationWorklist worklist, int batchSize, params OrderAllocation[] orders)
    {
        _orderAllocationRepository
            .Setup(r => r.GetWorklistAsync(OwnerId, worklist, batchSize, Token))
            .ReturnsAsync(orders);
    }

    // matches the requested SKU ids as a set, so the lookup may come in any order
    private void GivenSkus(params Sku[] skus)
    {
        var skuIds = skus.Select(s => s.Id).ToHashSet();

        _inventoryRepository
            .Setup(r => r.GetSkusByIdAsync(
                It.Is<IEnumerable<EntityId>>(ids => ids.ToHashSet().SetEquals(skuIds)), Token))
            .ReturnsAsync(skus);
    }

    private static OrderLineAllocation CreateLine(Sku sku, int quantityRequested = 1)
    {
        return OrderLineAllocation.Create(EntityId.Create(), sku.Id, quantityRequested).Value
               ?? throw new InvalidOperationException("test line could not be created");
    }

    private static OrderAllocation CreateOrder(NonFutureDateTime createdOn, params OrderLineAllocation[] lines)
    {
        return OrderAllocation.Create(OwnerId, Guid.CreateVersion7(), createdOn, lines).Value
               ?? throw new InvalidOperationException("test order could not be created");
    }

    private static void Allocate(OrderLineAllocation line, int quantity)
    {
        var receipt = SkuReceipt.Create(EntityId.Create(), line.SkuId, quantity, AppDateTime.Now).Value
                      ?? throw new InvalidOperationException("test receipt could not be created");

        line.AllocateReceipt(receipt, AppDateTime.Now);
    }

    #endregion
}
