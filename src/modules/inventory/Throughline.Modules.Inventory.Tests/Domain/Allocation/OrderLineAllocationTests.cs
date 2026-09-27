using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Tests.Domain.Allocation;

[Category("Unit")]
internal sealed class OrderLineAllocationTests
{
    [Test]
    public void IsAllocatable_NotFullyAllocated_ReturnsTrue()
    {
        var line = CreateLine(2);
        var receipt = CreateReceipt(line);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        Assert.That(line.IsAllocatable, Is.True);
    }

    [Test]
    public void IsAllocatable_FullyAllocated_ReturnsFalse()
    {
        var line = CreateLine(2);
        var receipt = CreateReceipt(line, 2);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        Assert.That(line.IsAllocatable, Is.False);
    }

    [TestCase(1, 2)]
    [TestCase(3, 0)]
    public void QuantityShort_AfterAllocating_ReturnsRemainder(int quantityAllocated, int expected)
    {
        var line = CreateLine(3);
        var receipt = CreateReceipt(line, quantityAllocated);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        Assert.That(line.QuantityShort, Is.EqualTo(expected));
    }

    #region Create

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_QuantityRequestedNotPositive_ReturnsValidationFailure(int quantityRequested)
    {
        var actual = OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), quantityRequested);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors.Single().Description,
                Is.EqualTo("quantityRequested must be greater than zero"));
        });
    }

    [Test]
    public void Create_ValidInput_ReturnsSuccess()
    {
        var orderLineId = EntityId.Create();
        var skuId = EntityId.Create();

        var actual = OrderLineAllocation.Create(orderLineId, skuId, 5);

        var line = actual.Value ?? throw new AssertionException("Create returned no value");

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(line.Id, Is.EqualTo(orderLineId));
            Assert.That(line.SkuId, Is.EqualTo(skuId));
            Assert.That(line.QuantityRequested, Is.EqualTo(5));
            Assert.That(line.QuantityAllocated, Is.Zero);
            Assert.That(line.LastUpdated, Is.Null);
        });
    }

    #endregion

    #region AllocationStatuses

    [Test]
    public void AllocationStatus_NoneAllocated_ReturnsConfirmed()
    {
        var line = CreateLine(2);

        Assert.That(line.AllocationStatus, Is.EqualTo(AllocationStatuses.Confirmed));
    }

    [Test]
    public void AllocationStatus_SomeAllocated_ReturnsPartiallyAllocated()
    {
        var line = CreateLine(2);
        var receipt = CreateReceipt(line);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        Assert.That(line.AllocationStatus, Is.EqualTo(AllocationStatuses.PartiallyAllocated));
    }

    [Test]
    public void AllocationStatus_AllAllocated_ReturnsAllocated()
    {
        var line = CreateLine(2);
        var receipt = CreateReceipt(line, 2);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        Assert.That(line.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));
    }

    #endregion


    #region Helpers

    private static OrderLineAllocation CreateLine(int quantityRequested)
    {
        return OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), quantityRequested)
                   .Value
               ?? throw new InvalidOperationException("test line could not be created");
    }

    private static SkuReceipt CreateReceipt(OrderLineAllocation orderLine, int quantityReceived = 1)
    {
        return SkuReceipt.Create(EntityId.Create(), orderLine.SkuId, quantityReceived, AppDateTime.Now)
            .Value!;
    }

    #endregion
}