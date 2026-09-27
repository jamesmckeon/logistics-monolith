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

    #region AllocateReceipt

    [Test]
    public void AllocateReceipt_DifferentSku_ThrowsExpected()
    {
        var line = CreateLine(1);
        var receipt = CreateReceipt(EntityId.Create());

        var ex = Assert.Throws<ArgumentException>(() => line.AllocateReceipt(receipt, AppDateTime.Now));

        Assert.Multiple(() =>
        {
            Assert.That(ex.ParamName, Is.EqualTo("receipt"));
            Assert.That(ex.Message, Does.StartWith("Receipt sku must be the same as the line's sku"));
        });
    }

    [Test]
    public void AllocateReceipt_FutureDate_ThrowsExpected()
    {
        var line = CreateLine(1);
        var receipt = CreateReceipt(line);

        var now = AppDateTime.Now.Add(new(0, 0, 0, 1));
        var ex = Assert.Throws<ArgumentException>(() => line.AllocateReceipt(receipt, now));

        Assert.Multiple(() =>
        {
            Assert.That(ex.ParamName, Is.EqualTo("allocatedOn"));
            Assert.That(ex.Message, Does.StartWith("allocatedOn must be in the past"));
        });
    }

    [Test]
    public void AllocateReceipt_LineAllocated_ThrowsExpected()
    {
        var line = CreateLine(1);
        var firstReceipt = CreateReceipt(line);
        line.AllocateReceipt(firstReceipt, AppDateTime.Now);

        var secondReceipt = CreateReceipt(line);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            line.AllocateReceipt(secondReceipt, AppDateTime.Now));

        Assert.That(ex.Message, Is.EqualTo("Cannot add a receipt to a fully allocated line"));
    }

    [Test]
    public void AllocateReceipt_ReceiptAllocated_ThrowsExpected()
    {
        var line = CreateLine(1);

        var receipt = CreateReceipt(line);
        receipt.Allocate(1, AppDateTime.Now);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            line.AllocateReceipt(receipt, AppDateTime.Now));


        Assert.That(ex.Message, Is.EqualTo("Cannot allocate a fully allocated receipt"));
    }

    [TestCase(2, 2)]
    [TestCase(3, 3)]
    [TestCase(4, 3)]
    public void AllocateReceipt_LineNotAllocated_AllocatesReceipt(
        int quantityReceived, int expectedAllocated)
    {
        var line = CreateLine(3);

        var receipt = CreateReceipt(line, quantityReceived);

        var now = AppDateTime.Now;
        line.AllocateReceipt(receipt, now);

        var allocatedReceipt = line.ReceiptAllocations.Single();
        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.EqualTo(expectedAllocated));
            Assert.That(allocatedReceipt.QuantityAllocated, Is.EqualTo(expectedAllocated));
            Assert.That(allocatedReceipt.AllocatedOn, Is.EqualTo(now));
            Assert.That(allocatedReceipt.SkuReceiptId, Is.EqualTo(receipt.Id));
        });
    }

    #endregion

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

        Assert.That(actual.Value, Is.Not.Null);

        var line = actual.Value;
        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(line.Id, Is.EqualTo(orderLineId));
            Assert.That(line.SkuId, Is.EqualTo(skuId));
            Assert.That(line.QuantityRequested, Is.EqualTo(5));
            Assert.That(line.QuantityAllocated, Is.Zero);
            Assert.That(line.QuantityShort, Is.EqualTo(5));
            Assert.That(line.LastUpdated, Is.Null);
        });
    }

    #endregion

    #region AllocationStatus

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

    private static SkuReceipt CreateReceipt(EntityId skuId, int quantityReceived = 1)
    {
        return SkuReceipt.Create(EntityId.Create(), skuId, quantityReceived, AppDateTime.Now)
            .Value!;
    }

    #endregion
}