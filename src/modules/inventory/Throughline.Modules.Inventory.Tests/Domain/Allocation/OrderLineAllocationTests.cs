using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Domain.Allocation;
using Throughline.Modules.Inventory.Domain.Common;

namespace Throughline.Modules.Inventory.Tests.Domain.Allocation;

[Category("Unit")]
internal sealed class OrderLineAllocationTests
{
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
    public void AllocationStatuses_NoneAllocated_ReturnsConfirmed()
    {
        var line = CreateLine(2);

        Assert.That(line.AllocationStatuses, Is.EqualTo(AllocationStatuses.Confirmed));
    }

    [Test]
    public void AllocationStatuses_SomeAllocated_ReturnsPartiallyAllocated()
    {
        var line = CreateLine(2);
        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        Assert.That(line.AllocationStatuses, Is.EqualTo(AllocationStatuses.PartiallyAllocated));
    }

    [Test]
    public void AllocationStatuses_AllAllocated_ReturnsAllocated()
    {
        var line = CreateLine(2);
        line.IncreaseQuantityAllocated(2, AppDateTime.Now);

        Assert.That(line.AllocationStatuses, Is.EqualTo(AllocationStatuses.Allocated));
    }

    #endregion

    [TestCase(0)]
    [TestCase(1)]
    public void IsAllocatable_NotFullyAllocated_ReturnsTrue(int quantityAllocated)
    {
        var line = CreateLine(2);
        line.IncreaseQuantityAllocated(quantityAllocated, AppDateTime.Now);

        Assert.That(line.IsAllocatable, Is.True);
    }

    [Test]
    public void IsAllocatable_FullyAllocated_ReturnsFalse()
    {
        var line = CreateLine(2);
        line.IncreaseQuantityAllocated(2, AppDateTime.Now);

        Assert.That(line.IsAllocatable, Is.False);
    }

    [TestCase(0, 3)]
    [TestCase(1, 2)]
    [TestCase(3, 0)]
    public void QuantityShort_AfterAllocating_ReturnsRemainder(int quantityAllocated, int expected)
    {
        var line = CreateLine(3);
        line.IncreaseQuantityAllocated(quantityAllocated, AppDateTime.Now);

        Assert.That(line.QuantityShort, Is.EqualTo(expected));
    }

    #region IncreaseQuantityAllocated

    [Test]
    public void IncreaseQuantityAllocated_WithinRequested_SetsExpected()
    {
        var line = CreateLine(3);

        var updatedOn = AppDateTime.Now;
        line.IncreaseQuantityAllocated(2, updatedOn);

        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.EqualTo(2));
            Assert.That(line.LastUpdated, Is.EqualTo(updatedOn));
        });
    }

    [Test]
    public void IncreaseQuantityAllocated_CalledTwice_AccumulatesQuantity()
    {
        var line = CreateLine(3);

        var firstUpdate = AppDateTime.Now.Subtract(new(1, 0, 0));
        line.IncreaseQuantityAllocated(1, firstUpdate);

        var secondUpdate = AppDateTime.Now;
        line.IncreaseQuantityAllocated(2, secondUpdate);

        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.EqualTo(3));
            Assert.That(line.LastUpdated, Is.EqualTo(secondUpdate));
        });
    }

    [Test]
    public void IncreaseQuantityAllocated_ZeroQuantity_SetsNothing()
    {
        var line = CreateLine(2);

        line.IncreaseQuantityAllocated(0, AppDateTime.Now);

        Assert.Multiple(() =>
        {
            Assert.That(line.QuantityAllocated, Is.Zero);
            Assert.That(line.LastUpdated, Is.Null);
        });
    }

    [Test]
    public void IncreaseQuantityAllocated_ExceedsRequested_ThrowsAndSetsNothing()
    {
        var line = CreateLine(2);

        Assert.Multiple(() =>
        {
            Assert.That(() => line.IncreaseQuantityAllocated(3, AppDateTime.Now),
                Throws.InvalidOperationException.With.Message.EqualTo(
                    "Increasing the allocated quantity by 3 " +
                    "would exceed the requested quantity for this order line"));
            Assert.That(line.QuantityAllocated, Is.Zero);
            Assert.That(line.LastUpdated, Is.Null);
        });
    }

    [Test]
    public void IncreaseQuantityAllocated_NegativeQuantity_ThrowsAndSetsNothing()
    {
        var line = CreateLine(2);

        Assert.Multiple(() =>
        {
            Assert.That(() => line.IncreaseQuantityAllocated(-1, AppDateTime.Now),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("quantity"));
            Assert.That(line.QuantityAllocated, Is.Zero);
            Assert.That(line.LastUpdated, Is.Null);
        });
    }

    [Test]
    public void IncreaseQuantityAllocated_FullyAllocated_ThrowsInvalidOperationException()
    {
        var line = CreateLine(1);
        line.IncreaseQuantityAllocated(1, AppDateTime.Now);

        Assert.That(() => line.IncreaseQuantityAllocated(1, AppDateTime.Now),
            Throws.InvalidOperationException.With.Message.EqualTo("line isn't allocatable"));
    }

    #endregion

    #region Helpers

    private static OrderLineAllocation CreateLine(int quantityRequested)
    {
        return OrderLineAllocation.Create(EntityId.Create(), EntityId.Create(), quantityRequested)
                   .Value
               ?? throw new InvalidOperationException("test line could not be created");
    }

    #endregion
}
