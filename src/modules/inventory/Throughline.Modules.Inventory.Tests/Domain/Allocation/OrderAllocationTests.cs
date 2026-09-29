using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Receiving;

namespace Throughline.Modules.Inventory.Tests.Domain.Allocation;

[Category("Unit")]
internal sealed class OrderAllocationTests
{
    [Test]
    public void UnallocatedLines_HasUnallocated_ReturnsExpected()
    {
        var partialLine = CreateLine(2);
        var receipt = CreateReceipt(partialLine);
        partialLine.AllocateReceipt(receipt, AppDateTime.Now);

        var allocatedLine = CreateLine();
        var receiptTwo = CreateReceipt(allocatedLine);
        allocatedLine.AllocateReceipt(receiptTwo, AppDateTime.Now);

        var confirmedLine = CreateLine();
        var order = CreateOrder(partialLine, confirmedLine, allocatedLine);

        Assert.That(order.UnallocatedLines, Is.EquivalentTo([partialLine, confirmedLine]));
    }

    [Test]
    public void UnallocatedLines_AllConfirmed_ReturnsEmpty()
    {
        var allocatedLine = CreateLine();
        var receipt = CreateReceipt(allocatedLine);
        allocatedLine.AllocateReceipt(receipt, AppDateTime.Now);

        var order = CreateOrder(allocatedLine);

        Assert.That(order.UnallocatedLines, Is.Empty);
    }

    #region Create

    [Test]
    public void Create_EmptyLines_ReturnsValidationFailure()
    {
        var actual = OrderAllocation.Create(1, Guid.Empty, []);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors.Single().Description,
                Is.EqualTo("lines must contain one or more items"));
        });
    }

    [Test]
    public void Create_DuplicateSkus_ReturnsValidationFailure()
    {
        var lineOne = CreateLine();
        var duplicate = CreateLine(lineOne.SkuId, 2);

        var actual = OrderAllocation.Create(1, Guid.Empty, [lineOne, duplicate]);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors.Single().Description,
                Is.EqualTo("lines must contain unique Sku IDs"));
        });
    }

    [Test]
    public void Create_ValidInput_ReturnsSuccess()
    {
        var line = CreateLine();

        var ownerId = 99;
        var orderId = Guid.CreateVersion7();

        var actual = OrderAllocation.Create(ownerId, orderId, [line]);

        Assert.That(actual.Value, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value.Id, Is.EqualTo(orderId));
            Assert.That(actual.Value.OwnerId, Is.EqualTo(ownerId));
            Assert.That(actual.Value.OrderLines, Is.EquivalentTo([line]));
        });
    }

    #endregion

    #region StartAllocating

    [Test]
    public void StartAllocating_NotAllocating_SetsExpected()
    {
        var order = CreateOrder(CreateLine());

        Assert.That(order.Allocating, Is.False);

        var lastUpdated = AppDateTime.Now;
        order.StartAllocating(lastUpdated);

        Assert.Multiple(() =>
        {
            Assert.That(order.Allocating, Is.True);
            Assert.That(order.LastUpdated, Is.EqualTo(lastUpdated));
        });
    }

    [Test]
    public void StartAllocating_AlreadyAllocating_SetsNothing()
    {
        var order = CreateOrder(CreateLine());

        var lastUpdated = AppDateTime.Now;
        order.StartAllocating(lastUpdated);

        Assert.That(order.Allocating, Is.True);

        var newUpdated = lastUpdated.Add(new(0, 0, 1));
        order.StartAllocating(newUpdated);

        Assert.That(order.LastUpdated, Is.EqualTo(lastUpdated));
    }

    #endregion

    #region StopAllocating

    [Test]
    public void StopAllocating_IsAllocating_SetsExpected()
    {
        var order = CreateOrder(CreateLine());
        var started = AppDateTime.Now.Subtract(new(1, 0, 0));
        order.StartAllocating(started);

        var stopped = AppDateTime.Now;
        order.StopAllocating(stopped);

        Assert.Multiple(() =>
        {
            Assert.That(order.Allocating, Is.False);
            Assert.That(order.LastUpdated, Is.EqualTo(stopped));
        });
    }

    [Test]
    public void StopAllocating_NotAllocating_SetsNothing()
    {
        var order = CreateOrder(CreateLine());

        Assert.That(order.Allocating, Is.False);

        var lastUpdated = AppDateTime.Now;
        order.StopAllocating(lastUpdated);

        Assert.That(order.LastUpdated, Is.Null);
    }

    #endregion

    #region AllocationStatus

    [Test]
    public void AllocationStatus_AllConfirmed_ReturnsConfirmed()
    {
        var line = CreateLine();
        var order = CreateOrder(line);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Confirmed));
    }

    [Test]
    public void AllocationStatus_OnePartialOneConfirmed_ReturnsPartial()
    {
        var line = CreateLine();
        var partialLine = CreateLine(2);
        var receipt = CreateReceipt(partialLine);
        partialLine.AllocateReceipt(receipt, AppDateTime.Now);

        var order = CreateOrder(line, partialLine);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.PartiallyAllocated));
    }

    [Test]
    public void AllocationStatus_OnePartialOneAllocated_ReturnsPartial()
    {
        var allocatedLine = CreateLine();
        var receipt = CreateReceipt(allocatedLine);
        allocatedLine.AllocateReceipt(receipt, AppDateTime.Now);

        var partialLine = CreateLine(2);
        var receiptTwo = CreateReceipt(partialLine);
        partialLine.AllocateReceipt(receiptTwo, AppDateTime.Now);

        var order = CreateOrder(allocatedLine, partialLine);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.PartiallyAllocated));
    }

    [Test]
    public void AllocationStatus_AllAllocated_ReturnsAllocated()
    {
        var line = CreateLine();
        var receipt = CreateReceipt(line);
        line.AllocateReceipt(receipt, AppDateTime.Now);

        var order = CreateOrder(line);

        Assert.That(order.AllocationStatus, Is.EqualTo(AllocationStatuses.Allocated));
    }

    #endregion


    #region Helpers

    private static OrderLineAllocation CreateLine(int quantityRequested = 1)
    {
        return OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), quantityRequested)
            .Value!;
    }

    private static OrderLineAllocation CreateLine(EntityId skuId, int quantityRequested = 1)
    {
        return OrderLineAllocation.Create(EntityId.Create(), skuId, quantityRequested)
            .Value!;
    }

    private static OrderAllocation CreateOrder(params OrderLineAllocation[] lines)
    {
        return OrderAllocation.Create(1, Guid.Empty, lines)
            .Value!;
    }

    private static InventoryPallet CreateReceipt(OrderLineAllocation line, int quantityReceived = 1)
    {
        return InventoryPallet.Create(EntityId.Create(), line.SkuId, quantityReceived, AppDateTime.Now)
            .Value!;
    }

    #endregion
}