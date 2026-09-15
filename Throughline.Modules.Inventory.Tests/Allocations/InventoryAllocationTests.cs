using Throughline.Common.Models;
using Throughline.Common.Results;
using Throughline.Modules.Inventory.Allocations;
using Throughline.Modules.Inventory.Domain.Owners;
using Throughline.Modules.Inventory.Domain.Skus;
using Throughline.Modules.Inventory.Receipts;

namespace Throughline.Modules.Inventory.Tests.Allocations;

[Category("Unit")]
public sealed class InventoryAllocationTests
{
    private static readonly Owner TestOwner = new(1, AllocationPolicies.Partial);
    private static readonly SkuCode TestSku = new("SKU-1");

    private static readonly AppDateTime AllocatedOn =
        new(new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero));

    private static readonly AppDateTime LaterAllocatedOn =
        new(new DateTimeOffset(2026, 9, 13, 9, 0, 0, TimeSpan.Zero));

    private static SkuReceipt Receipt(int quantity)
    {
        return new SkuReceipt(new OwnerSku(TestOwner, TestSku), quantity, AllocatedOn);
    }

    #region Constructor

    [Test]
    public void Constructor_NullReceipt_Throws()
    {
        Assert.That(
            () => new InventoryAllocation(null!, 1, AllocatedOn),
            Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_NullAllocatedOn_Throws()
    {
        Assert.That(
            () => new InventoryAllocation(Receipt(5), 1, null!),
            Throws.ArgumentNullException);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_QuantityNotPositive_Throws(int quantity)
    {
        Assert.That(
            () => new InventoryAllocation(Receipt(5), quantity, AllocatedOn),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Constructor_QuantityExceedsReceived_Throws()
    {
        Assert.That(
            () => new InventoryAllocation(Receipt(5), 6, AllocatedOn),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    #endregion

    #region Create

    [TestCase(0)]
    [TestCase(-1)]
    public void Create_QuantityNotPositive_ReturnsValidationFailure(int quantity)
    {
        var result = InventoryAllocation.Create(Receipt(5), quantity, AllocatedOn);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(
                result.Errors.Single().Description,
                Is.EqualTo("quantityAllocated must be greater than zero"));
        });
    }

    [Test]
    public void Create_QuantityExceedsReceived_ReturnsValidationFailure()
    {
        var result = InventoryAllocation.Create(Receipt(5), 6, AllocatedOn);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(
                result.Errors.Single().Description,
                Is.EqualTo("quantityAllocated can't exceed quantity received"));
        });
    }

    [Test]
    public void Create_QuantityEqualsReceived_ReturnsSuccess()
    {
        var result = InventoryAllocation.Create(Receipt(5), 5, AllocatedOn);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value, Is.Not.Null);
        });
    }

    [Test]
    public void Create_ValidInput_ReturnsSuccessWithValues()
    {
        var receipt = Receipt(5);

        var result = InventoryAllocation.Create(receipt, 3, AllocatedOn);

        Assert.That(result.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Receipt, Is.SameAs(receipt));
            Assert.That(result.Value.QuantityAllocated, Is.EqualTo(3));
            Assert.That(result.Value.AllocatedOn, Is.EqualTo(AllocatedOn));
        });
    }

    #endregion

    #region Equality

    [Test]
    public void Equals_DifferentReceipt_ReturnsFalse()
    {
        var a = new InventoryAllocation(Receipt(5), 2, AllocatedOn);
        var b = new InventoryAllocation(Receipt(5), 2, AllocatedOn);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equals_DifferentAllocatedOn_ReturnsFalse()
    {
        var receipt = Receipt(5);

        var a = new InventoryAllocation(receipt, 2, AllocatedOn);
        var b = new InventoryAllocation(receipt, 2, LaterAllocatedOn);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equals_SameReceiptAndDateDifferentQuantity_ReturnsTrue()
    {
        var receipt = Receipt(5);

        var a = new InventoryAllocation(receipt, 2, AllocatedOn);
        var b = new InventoryAllocation(receipt, 4, AllocatedOn);

        Assert.That(a, Is.EqualTo(b)); // quantity is not part of identity
    }

    #endregion
}
