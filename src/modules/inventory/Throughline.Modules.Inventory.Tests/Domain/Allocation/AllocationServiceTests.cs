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

    #region CanSatisfySpecification

    [TestCase(AllocationPolicies.Partial, true)]
    [TestCase(AllocationPolicies.ShipComplete, false)]
    public void CanSatisfySpecification_NoMatchingReceipts_ReturnsExpected(AllocationPolicies policy, bool expected)
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
    public void CanSatisfySpecification_InsufficientReceipts_ReturnsExpected(AllocationPolicies policy, bool expected)
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
    public void CanSatisfySpecification_SatisfiedBySingleReceipt_ReturnsTrue(AllocationPolicies policy)
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
    public void CanSatisfySpecification_SatisfiedByMultipleReceipts_ReturnsTrue(AllocationPolicies policy)
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
    public void CanSatisfySpecification_OrderAlreadyAllocated_ReturnsTrue(AllocationPolicies policy)
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        var order = OrderAllocation.Create(1, Guid.Empty, [line])
            .Value!;

        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatus.Allocated));

        var actual = _sut.CanSatisfyPolicyWithCurrentReceipts(order, [], policy);

        Assert.That(actual, Is.True);
    }

    #endregion
}