using Throughline.Common.Models;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Tests.Domain.Allocation;

[Category("Unit")]
public sealed class TotalAllocationSpecTests
{
    [Test]
    public void IsSatisfiedBy_OrderConfirmed_ReturnsFalse()
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;

        var allocation = OrderAllocation.Create(1, Guid.NewGuid(), [line])
            .Value!;
        var sut = new TotalAllocationSpec();

        Assert.That(allocation.AllocationStatus, Is.EqualTo(AllocationStatus.Confirmed));
        Assert.That(sut.IsSatisfiedBy(allocation), Is.False);
    }

    [Test]
    public void IsSatisfiedBy_OrderPartiallyAllocated_ReturnsFalse()
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 2)
            .Value!;
        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        var allocation = OrderAllocation.Create(1, Guid.NewGuid(), [line])
            .Value!;
        var sut = new TotalAllocationSpec();

        Assert.That(allocation.AllocationStatus, Is.EqualTo(AllocationStatus.PartiallyAllocated));
        Assert.That(sut.IsSatisfiedBy(allocation), Is.False);
    }

    [Test]
    public void IsSatisfiedBy_FullyAllocated_ReturnsTrue()
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), 1)
            .Value!;
        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        var allocation = OrderAllocation.Create(1, Guid.NewGuid(), [line])
            .Value!;
        var sut = new TotalAllocationSpec();

        Assert.That(allocation.AllocationStatus, Is.EqualTo(AllocationStatus.Allocated));
        Assert.That(sut.IsSatisfiedBy(allocation), Is.True);
    }
}