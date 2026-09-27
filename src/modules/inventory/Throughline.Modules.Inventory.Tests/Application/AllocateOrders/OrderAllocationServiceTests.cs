using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Throughline.Common.Models;
using Throughline.Modules.Inventory.Application.AllocateOrders;
using Throughline.Modules.Inventory.Application.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Infrastructure.Common;

namespace Throughline.Modules.Inventory.Tests.Application.AllocateOrders;

[Category("Unit")]
internal sealed class OrderAllocationServiceTests
{
    private const int OwnerId = 1;

    private static readonly CancellationTokenSource TokenSource = new();

    private Mock<IAllocationService> _allocationService;
    private Mock<IInventoryRepository> _inventoryRepository;
    private Mock<ILogger<OrderAllocationService>> _logger;
    private Mock<IOrderAllocationRepository> _orderAllocationRepository;
    private OrderAllocationService _sut;
    private Mock<IDbContextTransaction> _transaction;
    private Mock<IUnitOfWork> _unitOfWork;
    private static CancellationToken Token => TokenSource.Token;

    [SetUp]
    public void SetUp()
    {
        _allocationService = new Mock<IAllocationService>(MockBehavior.Strict);
        _inventoryRepository = new Mock<IInventoryRepository>(MockBehavior.Strict);
        _logger = new Mock<ILogger<OrderAllocationService>>(MockBehavior.Strict);
        _orderAllocationRepository = new Mock<IOrderAllocationRepository>(MockBehavior.Strict);
        _transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        _sut = new OrderAllocationService(
            _orderAllocationRepository.Object,
            _unitOfWork.Object,
            _allocationService.Object,
            _inventoryRepository.Object,
            _logger.Object);
    }

    #region AllocateOrderAsync

    [Test]
    public async Task AllocateOrderAsync_OrderAlreadyAllocating_ReturnsOrderAllocatingFailure()
    {
        var order = CreateOrder(CreateLine());
        order.StartAllocating(AppDateTime.Now);

        var actual = await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.OrderId, Is.EqualTo(order.Id));
            Assert.That(actual.Status, Is.EqualTo("failed"));
            Assert.That(actual.Errors, Is.EqualTo([AllocationError.OrderAllocating(order.Id)]));
        });
    }

    [Test]
    public async Task AllocateOrderAsync_OrderFullyAllocated_ReturnsFullyAllocated()
    {
        var line = CreateLine();
        var order = CreateOrder(line);
        line.AllocateReceipt(CreateReceipt(line.SkuId), AppDateTime.Now);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));

        var expected = AllocatedOrder.FullyAllocated(order.Id);

        var actual = await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.OrderId, Is.EqualTo(expected.OrderId));
            Assert.That(actual.Status, Is.EqualTo(expected.Status));
            Assert.That(actual.Shortages, Is.Empty);
            Assert.That(actual.Errors, Is.Empty);
        });
    }

    [Test]
    public async Task AllocateOrderAsync_ShipCompleteFailed_ReturnsFailed()
    {
        throw new NotImplementedException(
            "Verify that SUT returns AllocatedOrder.FailedStatus when order can't be fully allocated and policy is ShipComplete");
    }

    [Test]
    public async Task AllocateOrderAsync_ConcurrencyConflictOnStart_ReturnsOrderAllocatingFailure()
    {
        var order = CreateOrder(CreateLine());
        _unitOfWork.Setup(u => u.SaveChangesAsync(Token))
            .ThrowsAsync(new DbUpdateConcurrencyException("conflict"));

        var actual = await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.OrderId, Is.EqualTo(order.Id));
            Assert.That(actual.Status, Is.EqualTo("failed"));
            Assert.That(actual.Errors, Is.EqualTo([AllocationError.OrderAllocating(order.Id)]));
        });
    }

    [Test]
    public async Task AllocateOrderAsync_PolicyNotSatisfied_DoesNotAllocateOrCommit()
    {
        var line = CreateLine();
        var order = CreateOrder(line);
        var receipts = new[] { CreateReceipt(line.SkuId) };
        GivenAllocationStarts(order, receipts, AllocationPolicies.ShipComplete, false);
        GivenAllocationRollsBack();
        GivenResponse(order, receipts, [], AllocationStatuses.Confirmed, []);

        await _sut.AllocateOrderAsync(order, AllocationPolicies.ShipComplete, Token);

        // should save once each for StartAllocating() and StopAllocating()
        _unitOfWork.Verify(u => u.SaveChangesAsync(Token), Times.Exactly(2));
        _transaction.Verify(t => t.RollbackAsync(Token), Times.Once);
    }

    [Test]
    public async Task AllocateOrderAsync_PolicyNotSatisfied_StopsAllocating()
    {
        var line = CreateLine();
        var order = CreateOrder(line);
        var receipts = new[] { CreateReceipt(line.SkuId) };
        GivenAllocationStarts(order, receipts, AllocationPolicies.ShipComplete, false);
        GivenAllocationRollsBack();
        GivenResponse(order, receipts, [], AllocationStatuses.Confirmed, []);

        await _sut.AllocateOrderAsync(order, AllocationPolicies.ShipComplete, Token);

        Assert.That(order.Allocating, Is.False);
    }

    [Test]
    public async Task AllocateOrderAsync_PolicySatisfied_AllocatesEachUnallocatedLine()
    {
        var lineOne = CreateLine();
        var lineTwo = CreateLine();
        var order = CreateOrder(lineOne, lineTwo);
        var receipts = new[] { CreateReceipt(lineOne.SkuId), CreateReceipt(lineTwo.SkuId) };
        GivenAllocationStarts(order, receipts, AllocationPolicies.Partial, true);
        GivenLinesAllocated(order, receipts, lineOne, lineTwo);
        GivenAllocationCommits();
        GivenResponse(order, receipts, [], AllocationStatuses.Allocated, []);

        await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        _allocationService.Verify(a => a.AllocateOrderLine(order.Id, lineOne, receipts), Times.Once);
        _allocationService.Verify(a => a.AllocateOrderLine(order.Id, lineTwo, receipts), Times.Once);
    }

    [Test]
    public async Task AllocateOrderAsync_PolicySatisfied_SavesAllocationsBeforeCommitting()
    {
        var line = CreateLine();
        var order = CreateOrder(line);
        var receipts = new[] { CreateReceipt(line.SkuId) };

        var sequence = new MockSequence();
        _unitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync(Token)).Returns(Task.CompletedTask);
        _unitOfWork.InSequence(sequence).Setup(u => u.BeginTransactionAsync(Token))
            .ReturnsAsync(_transaction.Object);
        _inventoryRepository.InSequence(sequence)
            .Setup(r => r.GetAvailableInventoryAsync(
                It.Is<IEnumerable<EntityId>>(ids => ids.SequenceEqual(new[] { line.SkuId })), Token))
            .ReturnsAsync(receipts);
        _allocationService.InSequence(sequence)
            .Setup(a => a.CanSatisfyPolicyWithCurrentReceipts(order, receipts, AllocationPolicies.Partial))
            .Returns(true);
        _allocationService.InSequence(sequence).Setup(a => a.AllocateOrderLine(order.Id, line, receipts));
        _unitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync(Token)).Returns(Task.CompletedTask);
        _transaction.InSequence(sequence).Setup(t => t.CommitAsync(Token)).Returns(Task.CompletedTask);
        GivenResponse(order, receipts, [], AllocationStatuses.Allocated, []);

        await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        _transaction.Verify(t => t.CommitAsync(Token), Times.Once);
    }

    [Test]
    public async Task AllocateOrderAsync_PolicySatisfied_StopsAllocating()
    {
        var line = CreateLine();
        var order = CreateOrder(line);
        var receipts = new[] { CreateReceipt(line.SkuId) };
        GivenAllocationStarts(order, receipts, AllocationPolicies.Partial, true);
        GivenLinesAllocated(order, receipts, line);
        GivenAllocationCommits();
        GivenResponse(order, receipts, [], AllocationStatuses.Allocated, []);

        await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        Assert.That(order.Allocating, Is.False);
    }

    [TestCase(AllocationStatuses.Allocated, "fullyAllocated")]
    [TestCase(AllocationStatuses.PartiallyAllocated, "partiallyAllocated")]
    [TestCase(AllocationStatuses.Confirmed, "notAllocated")]
    public async Task AllocateOrderAsync_DerivedStatus_ReturnsMatchingStatus(
        AllocationStatuses derivedStatus, string expected)
    {
        var line = CreateLine();
        var order = CreateOrder(line);
        var receipts = new[] { CreateReceipt(line.SkuId) };
        GivenAllocationStarts(order, receipts, AllocationPolicies.Partial, true);
        GivenLinesAllocated(order, receipts, line);
        GivenAllocationCommits();
        GivenResponse(order, receipts, [], derivedStatus, []);

        var actual = await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.OrderId, Is.EqualTo(order.Id));
            Assert.That(actual.Status, Is.EqualTo(expected));
        });
    }

    [Test]
    public async Task AllocateOrderAsync_HasShortages_ReturnsShortagesBySkuCode()
    {
        var line = CreateLine(3);
        var order = CreateOrder(line);
        var receipts = new[] { CreateReceipt(line.SkuId) };
        var shortage = new SkuIdShortage(line.SkuId, 3, 1, 2);
        var sku = new Sku(line.SkuId, OwnerId, "SKU-A");
        GivenAllocationStarts(order, receipts, AllocationPolicies.Partial, true);
        GivenLinesAllocated(order, receipts, line);
        GivenAllocationCommits();
        GivenResponse(order, receipts, [shortage], AllocationStatuses.PartiallyAllocated, [sku]);

        var actual = await _sut.AllocateOrderAsync(order, AllocationPolicies.Partial, Token);

        Assert.That(actual.Shortages, Is.EqualTo([new SkuCodeShortage("SKU-A", 3, 1, 2)]));
    }

    #endregion

    #region Helpers

    private void GivenAllocationStarts(
        OrderAllocation order, SkuReceipt[] receipts, AllocationPolicies policy, bool canSatisfyPolicy)
    {
        var skuIds = order.UnallocatedLines.Select(l => l.SkuId).ToArray();

        _unitOfWork.Setup(u => u.SaveChangesAsync(Token)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.BeginTransactionAsync(Token)).ReturnsAsync(_transaction.Object);
        _inventoryRepository
            .Setup(r => r.GetAvailableInventoryAsync(
                It.Is<IEnumerable<EntityId>>(ids => ids.SequenceEqual(skuIds)), Token))
            .ReturnsAsync(receipts);
        _allocationService
            .Setup(a => a.CanSatisfyPolicyWithCurrentReceipts(order, receipts, policy))
            .Returns(canSatisfyPolicy);
    }

    private void GivenLinesAllocated(
        OrderAllocation order, SkuReceipt[] receipts, params OrderLineAllocation[] lines)
    {
        foreach (var line in lines)
            _allocationService.Setup(a => a.AllocateOrderLine(order.Id, line, receipts));
    }

    private void GivenAllocationCommits()
    {
        _transaction.Setup(t => t.CommitAsync(Token)).Returns(Task.CompletedTask);
    }

    private void GivenAllocationRollsBack()
    {
        _transaction.Setup(t => t.RollbackAsync(Token))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.ClearChanges());
    }

    private void GivenResponse(
        OrderAllocation order,
        SkuReceipt[] receipts,
        SkuIdShortage[] shortages,
        AllocationStatuses derivedStatus,
        Sku[] skus)
    {
        var shortageSkuIds = shortages.Select(s => s.SkuId).ToArray();

        _allocationService.Setup(a => a.GetShortedSkus(order, receipts)).Returns(shortages);
        _allocationService
            .Setup(a => a.DeriveStatusFromShortages(
                order, It.Is<IEnumerable<SkuIdShortage>>(s => s.SequenceEqual(shortages))))
            .Returns(derivedStatus);
        _inventoryRepository
            .Setup(r => r.GetSkusByIdAsync(
                It.Is<IEnumerable<EntityId>>(ids => ids.SequenceEqual(shortageSkuIds)), Token))
            .ReturnsAsync(skus);
    }

    private static OrderLineAllocation CreateLine(int quantityRequested = 1)
    {
        return OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), quantityRequested).Value
               ?? throw new InvalidOperationException("test line could not be created");
    }

    private static OrderAllocation CreateOrder(params OrderLineAllocation[] lines)
    {
        return OrderAllocation.Create(OwnerId, Guid.CreateVersion7(), lines).Value
               ?? throw new InvalidOperationException("test order could not be created");
    }

    private static SkuReceipt CreateReceipt(EntityId skuId)
    {
        return SkuReceipt.Create(EntityId.Create(), skuId, 5, AppDateTime.Now.Subtract(new(1, 0, 0))).Value
               ?? throw new InvalidOperationException("test receipt could not be created");
    }

    #endregion
}