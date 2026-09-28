using Throughline.Common.Models;
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

    #region AllocateOrderLine

    [Test]
    public void AllocateOrderLine_NoSingleReceiptCoversLine_AllocatesAcrossReceipts()
    {
        var line = CreateLine(3);
        var orderId = Guid.CreateVersion7();
        var receiptOne = CreateReceipt(line.SkuId, AppDateTime.Now.Subtract(new(2, 0, 0)));
        var receiptTwo = CreateReceipt(line.SkuId, AppDateTime.Now.Subtract(new(1, 0, 0)), 2);

        _sut.AllocateOrderLine(orderId, line, [receiptOne, receiptTwo]);

        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.EqualTo(3));
            Assert.That(line.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));
            Assert.That(line.ReceiptAllocations.Count, Is.EqualTo(2));
            Assert.That(line.ReceiptAllocations.Any(a =>
                a.SkuReceiptId == receiptOne.Id && a.QuantityAllocated == 1));
            Assert.That(line.ReceiptAllocations.Any(a =>
                a.SkuReceiptId == receiptTwo.Id && a.QuantityAllocated == 2));
        });
    }

    [Test]
    public void AllocateOrderLine_ReceiptsExceedRequestedQuantity_AllocatesReceipts()
    {
        var line = CreateLine(3);
        var orderId = Guid.CreateVersion7();
        //verify FEFO
        var receiptOne = CreateReceipt(line.SkuId, AppDateTime.Now.Subtract(new(0, 0, 0, 1)), 2);
        var receiptTwo = CreateReceipt(line.SkuId, AppDateTime.Now, 2);

        _sut.AllocateOrderLine(orderId, line, [receiptOne, receiptTwo]);

        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.EqualTo(3));
            Assert.That(line.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));
            Assert.That(line.ReceiptAllocations.Count, Is.EqualTo(2));
            Assert.That(line.ReceiptAllocations.Single(a =>
                a.SkuReceiptId == receiptOne.Id).QuantityAllocated, Is.EqualTo(2));
            Assert.That(line.ReceiptAllocations.Single(a =>
                a.SkuReceiptId == receiptTwo.Id).QuantityAllocated, Is.EqualTo(1));
        });
    }

    [Test]
    public void AllocateOrderLine_ReceiptsInsufficientCombined_AllocatesAllAvailable()
    {
        var line = CreateLine(5);
        var orderId = Guid.CreateVersion7();
        var receiptOne = CreateReceipt(line.SkuId, AppDateTime.Now.Subtract(new(2, 0, 0)));
        var receiptTwo = CreateReceipt(line.SkuId, AppDateTime.Now.Subtract(new(1, 0, 0)), 2);

        _sut.AllocateOrderLine(orderId, line, [receiptOne, receiptTwo]);

        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.EqualTo(3));
            Assert.That(line.AllocationStatus, Is.EqualTo(AllocationStatuses.PartiallyAllocated));
            Assert.That(receiptOne.QuantityAvailable, Is.Zero);
            Assert.That(receiptTwo.QuantityAvailable, Is.Zero);
        });
    }

    [Test]
    public void AllocateOrderLine_NoReceiptsMatchLineSku_LeavesLineUnallocated()
    {
        var line = CreateLine(3);
        var orderId = Guid.CreateVersion7();
        // a receipt for a different sku, e.g. another line on the same order
        var otherSkuReceipt = CreateReceipt(EntityId.Create(), 5);

        _sut.AllocateOrderLine(orderId, line, [otherSkuReceipt]);

        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.Zero);
            Assert.That(line.AllocationStatus, Is.EqualTo(AllocationStatuses.Confirmed));
            Assert.That(line.ReceiptAllocations, Is.Empty);
            Assert.That(otherSkuReceipt.QuantityAvailable, Is.EqualTo(5));
        });
    }

    #endregion

    #region DeriveStatusFromShortages

    [Test]
    public void DeriveStatusFromShortages_AllLinesAllocated_ReturnsAllocated()
    {
        var line = CreateLine();
        var receipt = CreateReceipt(line);
        line.AllocateReceipt(receipt, AppDateTime.Now);

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
        var order = CreateOrder(lineOne, lineTwo);

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
        var line = CreateLine();
        var order = CreateOrder(line);
        var receipt = CreateReceipt(line);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        var actual = _sut.GetShortedSkus(order, [CreateReceipt(line.SkuId)]);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));
        Assert.That(actual, Is.Empty);
    }

    [Test]
    public void GetShortedSkus_NoMatchingReceipts_ReturnsShortedLines()
    {
        var shortedLine = CreateLine();
        // create one allocated line - this shouldn't be returned
        var allocatedLine = CreateLine();
        var allocatedReceipt = CreateReceipt(allocatedLine);
        allocatedLine.AllocateReceipt(allocatedReceipt, AppDateTime.Now);

        var order = CreateOrder(shortedLine, allocatedLine);

        var receipt = CreateReceipt(allocatedLine.SkuId);

        var expectedShortage = new SkuIdShortage(shortedLine.SkuId, shortedLine.QuantityRequested,
            shortedLine.QuantityAllocated, shortedLine.QuantityShort);

        var actual = _sut.GetShortedSkus(order, [receipt]);

        Assert.That(actual, Is.EquivalentTo([expectedShortage]));
    }

    [Test]
    public void GetShortedSkus_MatchingReceipts_ReturnsShortedLines()
    {
        var shortedLine = CreateLine(2);
        // create one allocated line - this shouldn't be returned
        var allocatedLine = CreateLine();
        var allocatedReceipt = CreateReceipt(allocatedLine);
        allocatedLine.AllocateReceipt(allocatedReceipt, AppDateTime.Now);

        var order = CreateOrder(shortedLine, allocatedLine);

        var receipt = CreateReceipt(shortedLine);

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
        // to another order, to ensure that SUT is using the available quantity
        // on the receipt, not its received quantity
        var otherLine = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var skuReceipt = CreateReceipt(otherLine, 2);
        otherLine.AllocateReceipt(skuReceipt, AppDateTime.Now);

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [skuReceipt], policy);

        Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    ///     Verifies that SUT handles case where receipts doesn't contain an item for the sku that is
    ///     on the line being tested
    /// </summary>
    [TestCase(AllocationPolicies.Partial, true)]
    [TestCase(AllocationPolicies.ShipComplete, false)]
    public void CanSatisfyPolicyWithCurrentReceipts_SkuNotInReceipts_ReturnsExpected(
        AllocationPolicies policy, bool expected)
    {
        var line = CreateLine();
        var lineTwo = CreateLine();
        var order = OrderAllocation.Create(1, Guid.Empty, [line, lineTwo])
            .Value!;

        // create a receipt for the sku on line, but not one for the sku
        // on lineTwo
        var skuReceipt = CreateReceipt(line);

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(
            order, [skuReceipt], policy);

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
        var otherReceipt = CreateReceipt(otherLine);

        otherLine.AllocateReceipt(otherReceipt, AppDateTime.Now);

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
        var receipt = CreateReceipt(line);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [], policy);

        Assert.That(actual, Is.True);
    }

    #endregion

    #region Helpers

    private static SkuReceipt CreateReceipt(EntityId skuId, int quantityReceived = 1)
    {
        return SkuReceipt.Create(EntityId.Create(), skuId, quantityReceived, AppDateTime.Now)
            .Value!;
    }

    private static SkuReceipt CreateReceipt(EntityId skuId, AppDateTime receivedOn, int quantityReceived = 1)
    {
        return SkuReceipt.Create(EntityId.Create(), skuId, quantityReceived, receivedOn)
            .Value!;
    }

    private static SkuReceipt CreateReceipt(OrderLineAllocation orderLine, int quantityReceived = 1)
    {
        return SkuReceipt.Create(EntityId.Create(), orderLine.SkuId, quantityReceived, AppDateTime.Now)
            .Value!;
    }


    private static OrderLineAllocation CreateLine(int quantityRequested = 1)
    {
        return OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), quantityRequested)
            .Value!;
    }

    private static OrderAllocation CreateOrder(params OrderLineAllocation[] lines)
    {
        return OrderAllocation.Create(1, Guid.Empty, lines).Value!;
    }

    #endregion
}