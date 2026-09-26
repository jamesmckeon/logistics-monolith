using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Tests.Domain.Allocation;

[Category("Unit")]
internal sealed class AllocationServiceTests
{
    private AllocationService _sut { get; set; }

    [SetUp]
    public void Setup()
    {
        _sut = new();
    }

    #region DeriveStatusFromShortages

    [Test]
    public void DeriveStatusFromShortages_AllLinesAllocated_ReturnsAllocated()
    {
        var line = CreateLine();
        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        var order = CreateOrder(line);
        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));

        // this shouldn't ever happen, but the sut should be able to handle as per its contract
        var shortage = new SkuIdShortage(line.SkuId, line.QuantityRequested, line.QuantityRequested - 1,
            line.QuantityRequested);

        var actual = _sut.DeriveStatusFromShortages(order, [shortage]);

        Assert.That(actual, Is.EqualTo(AllocationStatuses.Allocated));
    }

    [Test]
    public void DeriveStatusFromShortages_NoMatchingShortages_ReturnsConfirmed()
    {
        var line = CreateLine();
        var order = CreateOrder(line);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Confirmed));

        var shortage = new SkuIdShortage(EntityId.Create(), 2, 0, 2);

        var actual = _sut.DeriveStatusFromShortages(order, [shortage]);

        Assert.That(actual, Is.EqualTo(AllocationStatuses.Confirmed));
    }

    [Test]
    public void DeriveStatusFromShortages_HasMatchingShortage_ReturnsPartiallyAllocated()
    {
        var line = CreateLine(2);
        var order = CreateOrder(line);

        // create one shortage that would result in the line's requested quantity
        // being partially fulfilled if applied
        var shortage = new SkuIdShortage(line.SkuId, line.QuantityRequested, 1, 1);

        var actual = _sut.DeriveStatusFromShortages(order, [shortage]);

        Assert.That(actual, Is.EqualTo(AllocationStatuses.PartiallyAllocated));
    }

    [Test]
    public void DeriveStatusFromShortages_HasMatchingShortage_ReturnsAllocated()
    {
        var line = CreateLine();
        var order = CreateOrder(line);

        var shortage = new SkuIdShortage(line.SkuId, line.QuantityRequested, 1, 1);

        var actual = _sut.DeriveStatusFromShortages(order, [shortage]);

        Assert.That(actual, Is.EqualTo(AllocationStatuses.Allocated));
    }

    [Test]
    public void DeriveStatusFromShortages_HasMultipleUnallocatedLines_ReturnsPartiallyAllocated()
    {
        var lineOne = CreateLine();
        var lineTwo = CreateLine();
        var order = CreateOrder([lineOne, lineTwo]);

        var shortage = new SkuIdShortage(lineOne.SkuId, lineOne.QuantityRequested, 1, 1);

        var actual = _sut.DeriveStatusFromShortages(order, [shortage]);

        Assert.That(actual, Is.EqualTo(AllocationStatuses.PartiallyAllocated));
    }

    [Test]
    public void DeriveStatusFromShortages_InvalidShortage_ThrowsExpected()
    {
        var line = CreateLine();
        var order = CreateOrder(line);

        // create a shortage with an allocated quantity that would exceed the line's
        // requested quantity if applied
        var shortage = new SkuIdShortage(line.SkuId, line.QuantityRequested, 99, 1);

        var ex = Assert.Throws<InvalidOperationException>(() => _sut.DeriveStatusFromShortages(order, [shortage]));

        Assert.That(ex.Message, Is.EqualTo("At least one shortage contains a QuantityAllocated that would " +
                                           "exceed a line's QuantityRequested"));
    }

    #endregion

    #region GetShortedSkus

    [Test]
    public void GetShortedSkus_NoShortedLines_ReturnsEmptyShortages()
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var order = OrderAllocation.Create(1, Guid.Empty, [line])
            .Value!;

        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        var receipt = SkuReceipt.Create(EntityId.Create(), EntityId.Create(), 1, AppDateTime.Now)
            .Value!;

        var actual = _sut.GetShortedSkus(order, [receipt]);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));
        Assert.That(actual, Is.Empty);
    }

    [Test]
    public void GetShortedSkus_NoMatchingReceipts_ReturnsShortedLines()
    {
        var shortedLine = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        // create one allocated line - this shouldn't be returned
        var allocatedLine = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        allocatedLine.IncreaseQuantityAllocated(1, AppDateTime.Now);
        var order = OrderAllocation.Create(1, Guid.Empty, [shortedLine, allocatedLine])
            .Value!;

        var receipt = SkuReceipt.Create(EntityId.Create(), EntityId.Create(), 1, AppDateTime.Now)
            .Value!;

        var expectedShortage = new SkuIdShortage(shortedLine.SkuId, shortedLine.QuantityRequested,
            shortedLine.QuantityAllocated, shortedLine.QuantityShort);

        var actual = _sut.GetShortedSkus(order, [receipt]);

        Assert.That(actual, Is.EquivalentTo([expectedShortage]));
    }

    [Test]
    public void GetShortedSkus_MatchingReceipts_ReturnsShortedLines()
    {
        var shortedLine = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 2)
            .Value!;
        // create one allocated line - this shouldn't be returned
        var allocatedLine = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        allocatedLine.IncreaseQuantityAllocated(1, AppDateTime.Now);
        var order = OrderAllocation.Create(1, Guid.Empty, [shortedLine, allocatedLine])
            .Value!;

        var receipt = SkuReceipt.Create(EntityId.Create(), shortedLine.SkuId, 1, AppDateTime.Now)
            .Value!;

        // sut should apply available quantity on receipt to line
        var expectedShortage = new SkuIdShortage(shortedLine.SkuId, shortedLine.QuantityRequested,
            shortedLine.QuantityAllocated + 1, shortedLine.QuantityShort - 1);

        var actual = _sut.GetShortedSkus(order, [receipt]);

        Assert.That(actual, Is.EquivalentTo([expectedShortage]));
    }

    #endregion

    #region CanSatisfyPolicyWithCurrentReceipts

    [TestCase(AllocationPolicies.Partial, true)]
    [TestCase(AllocationPolicies.ShipComplete, false)]
    public void CanSatisfyPolicyWithCurrentReceipts_NoMatchingReceipts_ReturnsExpected(AllocationPolicies policy,
        bool expected)
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var order = OrderAllocation.Create(1, Guid.Empty, [line])
            .Value!;
        // receipt with a different sku id than on the order line
        var skuReceipt = SkuReceipt.Create(EntityId.Create(), EntityId.Create(), 1, AppDateTime.Now)
            .Value!;

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [skuReceipt], policy);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(AllocationPolicies.Partial, true)]
    [TestCase(AllocationPolicies.ShipComplete, false)]
    public void CanSatisfyPolicyWithCurrentReceipts_InsufficientReceipts_ReturnsExpected(AllocationPolicies policy,
        bool expected)
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 2)
            .Value!;
        var order = OrderAllocation.Create(1, Guid.Empty, [line])
            .Value!;

        // allocate the quantity that could potentially be used to complete the first order
        // to another order, to ensure that SUT is using the available/allocated quantity
        // on the receipt, not its received quantity
        var otherLine = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var otherOrder = OrderAllocation.Create(1, Guid.Empty, [otherLine])
            .Value!;
        var skuReceipt = SkuReceipt.Create(EntityId.Create(), line.SkuId, 2, AppDateTime.Now)
            .Value!;
        skuReceipt.AllocateToOrder(otherOrder, 1, AppDateTime.Now);

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [skuReceipt], policy);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(AllocationPolicies.Partial)]
    [TestCase(AllocationPolicies.ShipComplete)]
    public void CanSatisfyPolicyWithCurrentReceipts_SatisfiedBySingleReceipt_ReturnsTrue(AllocationPolicies policy)
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var order = OrderAllocation.Create(1, Guid.Empty, [line])
            .Value!;
        var skuReceipt = SkuReceipt.Create(EntityId.Create(), line.SkuId, 2, AppDateTime.Now)
            .Value!;

        // partially allocate receipt to another order to ensure SUT uses receipt's allocatable
        // quantity
        var otherLine = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var otherOrder = OrderAllocation.Create(1, Guid.Empty, [otherLine])
            .Value!;

        skuReceipt.AllocateToOrder(otherOrder, 1, AppDateTime.Now);

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [skuReceipt], policy);

        Assert.That(actual, Is.True);
    }

    [TestCase(AllocationPolicies.Partial)]
    [TestCase(AllocationPolicies.ShipComplete)]
    public void CanSatisfyPolicyWithCurrentReceipts_SatisfiedByMultipleReceipts_ReturnsTrue(AllocationPolicies policy)
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 3)
            .Value!;
        var order = OrderAllocation.Create(1, Guid.Empty, [line])
            .Value!;
        var receiptOne = SkuReceipt.Create(EntityId.Create(), line.SkuId, 1, AppDateTime.Now)
            .Value!;
        var receiptTwo = SkuReceipt.Create(EntityId.Create(), line.SkuId, 3, AppDateTime.Now)
            .Value!;

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [receiptOne, receiptTwo], policy);

        Assert.That(actual, Is.True);
    }

    [TestCase(AllocationPolicies.Partial)]
    [TestCase(AllocationPolicies.ShipComplete)]
    public void CanSatisfyPolicyWithCurrentReceipts_OrderAlreadyAllocated_ReturnsTrue(AllocationPolicies policy)
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var order = OrderAllocation.Create(1, Guid.Empty, [line])
            .Value!;

        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [], policy);

        Assert.That(actual, Is.True);
    }

    #endregion

    #region Helpers

    private static Result<SkuReceipt> CreateReceipt(EntityId skuId, int quantityReceived = 1)
    {
        return SkuReceipt.Create(EntityId.Create(), skuId, 1, AppDateTime.Now);
    }

    private static OrderLineAllocation CreateLine(int quantityRequested = 1)
    {
        return OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), quantityRequested)
            .Value!;
    }

    private static OrderAllocation CreateOrder(OrderLineAllocation line)
    {
        return OrderAllocation.Create(1, Guid.Empty, [line]).Value!;
    }

    private static OrderAllocation CreateOrder(OrderLineAllocation[] lines)
    {
        return OrderAllocation.Create(1, Guid.Empty, lines).Value!;
    }

    #endregion
}