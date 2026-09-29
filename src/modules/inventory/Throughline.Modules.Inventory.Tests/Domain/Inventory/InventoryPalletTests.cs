using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Receiving;

namespace Throughline.Modules.Inventory.Tests.Domain.Inventory;

[Category("Unit")]
internal sealed class InventoryPalletTests
{
    #region Allocate

    [Test]
    public void Allocate_QuantityAvailableExceeded_ThrowsExpected()
    {
        var recept = CreateReceipt(1);
        recept.Allocate(1, AppDateTime.Now);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => recept.Allocate(1, AppDateTime.Now));
        Assert.Multiple(() =>
        {
            Assert.That(ex.ParamName, Is.EqualTo("quantity"));
            Assert.That(ex.Message, Does.StartWith("quantity exceeds receipt's available quantity"));
        });
    }

    [Test]
    public void Allocate_NegativeQuantity_ThrowsExpected()
    {
        var recept = CreateReceipt(1);
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => recept.Allocate(-1, AppDateTime.Now));
        Assert.That(ex.ParamName, Is.EqualTo("quantity"));
    }

    [Test]
    public void Allocate_AllocatedOnInTheFuture_ThrowsExpected()
    {
        var recept = CreateReceipt(1);
        var allocatedOn = AppDateTime.Now.Add(new(0, 0, 0, 1));

        var ex = Assert.Throws<ArgumentException>(() => recept.Allocate(1, allocatedOn));

        Assert.Multiple(() =>
        {
            Assert.That(ex.ParamName, Is.EqualTo("allocatedOn"));
            Assert.That(ex.Message, Does.StartWith("allocatedOn cannot be in the future"));
        });
    }

    [Test]
    public void Allocate_HasAvailableQuantity_AllocatesQuantity()
    {
        var recept = CreateReceipt(1);
        var allocatedOn = AppDateTime.Now;

        recept.Allocate(1, allocatedOn);

        Assert.Multiple(() =>
        {
            Assert.That(recept.QuantityAvailable, Is.Zero);
            Assert.That(recept.LastUpdated, Is.EqualTo(allocatedOn));
            Assert.That(recept.QuantityAllocated, Is.EqualTo(1));
        });
    }

    [Test]
    public void Allocate_ZeroQuantity_DoesNothing()
    {
        var recept = CreateReceipt(1);

        recept.Allocate(0, AppDateTime.Now);

        Assert.Multiple(() =>
        {
            Assert.That(recept.QuantityAvailable, Is.EqualTo(1));
            Assert.That(recept.LastUpdated, Is.Null);
            Assert.That(recept.QuantityAllocated, Is.Zero);
        });
    }

    #endregion

    #region Create

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_QuantityReceivedNotPositive_ReturnsValidationFailure(int quantityReceived)
    {
        var actual = InventoryPallet.Create(EntityId.Create(), EntityId.Create(), quantityReceived,
            AppDateTime.Now.Subtract(new(1, 0, 0)));

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors.Single().Description,
                Is.EqualTo("quantityReceived must be greater than zero"));
        });
    }

    [Test]
    public void Create_ReceivedOnInFuture_ReturnsValidationFailure()
    {
        var actual = InventoryPallet.Create(EntityId.Create(), EntityId.Create(), 1,
            AppDateTime.Now.Add(new(1, 0, 0)));

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(actual.Errors.Single().Description,
                Is.EqualTo("receivedOn must be in the past"));
        });
    }

    [Test]
    public void Create_ValidInput_ReturnsSuccess()
    {
        var id = EntityId.Create();
        var skuId = EntityId.Create();
        var receivedOn = AppDateTime.Now.Subtract(new(1, 0, 0));

        var actual = InventoryPallet.Create(id, skuId, 5, receivedOn);

        var receipt = actual.Value ?? throw new AssertionException("Create returned no value");

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(receipt.Id, Is.EqualTo(id));
            Assert.That(receipt.SkuId, Is.EqualTo(skuId));
            Assert.That(receipt.QuantityReceived, Is.EqualTo(5));
            Assert.That(receipt.ReceivedOn, Is.EqualTo(receivedOn));
            Assert.That(receipt.QuantityAllocated, Is.Zero);
            Assert.That(receipt.QuantityAvailable, Is.EqualTo(5));
        });
    }

    #endregion

    #region Helpers

    private static InventoryPallet CreateReceipt(int quantityReceived)
    {
        return InventoryPallet.Create(EntityId.Create(), EntityId.Create(), quantityReceived,
                       AppDateTime.Now.Subtract(new(1, 0, 0)))
                   .Value
               ?? throw new InvalidOperationException("test receipt could not be created");
    }

    private static OrderAllocation CreateOrder(EntityId skuId)
    {
        var line = OrderLineAllocation.Create(EntityId.Create(), skuId, 2).Value
                   ?? throw new InvalidOperationException("test line could not be created");

        return OrderAllocation.Create(1, Guid.CreateVersion7(), [line]).Value
               ?? throw new InvalidOperationException("test order could not be created");
    }

    #endregion
}