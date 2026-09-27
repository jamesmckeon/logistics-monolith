using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;
using Throughline.Modules.Inventory.Domain.Inventory;

namespace Throughline.Modules.Inventory.Tests.Domain.Inventory;

[Category("Unit")]
internal sealed class SkuReceiptTests
{
    #region Create

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_QuantityReceivedNotPositive_ReturnsValidationFailure(int quantityReceived)
    {
        var actual = SkuReceipt.Create(EntityId.Create(), EntityId.Create(), quantityReceived,
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
        var actual = SkuReceipt.Create(EntityId.Create(), EntityId.Create(), 1,
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

        var actual = SkuReceipt.Create(id, skuId, 5, receivedOn);

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

    private static SkuReceipt CreateReceipt(int quantityReceived)
    {
        return SkuReceipt.Create(EntityId.Create(), EntityId.Create(), quantityReceived,
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