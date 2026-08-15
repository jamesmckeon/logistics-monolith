using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;

namespace Throughline.Modules.Ordering.Domain.Tests.OrderEstimates;

[Category("Unit")]
public sealed class OrderEstimateRequestItemTests
{
    #region Constructor

    [Test]
    public void Constructor_NullSkuCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OrderEstimateRequestItem(null!, 1, 1m, new Rate(1m)));
    }

    [Test]
    public void Constructor_NullUnitPickFee_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OrderEstimateRequestItem(new SkuCode("SKU-1"), 1, 1m, null!));
    }

    [TestCase("0")]
    [TestCase("-0.01")]
    public void Constructor_UnitWeightAtOrBelowZero_ThrowsArgumentOutOfRange(decimal unitWeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OrderEstimateRequestItem(new SkuCode("SKU-1"), 1, unitWeight, new Rate(1m)));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_TotalQuantityAtOrBelowZero_ThrowsArgumentOutOfRange(int totalQuantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OrderEstimateRequestItem(new SkuCode("SKU-1"), totalQuantity, 1m, new Rate(1m)));
    }

    [Test]
    public void Constructor_ValidArguments_ExposesSuppliedValues()
    {
        var skuCode = new SkuCode("SKU-1");
        var unitPickFee = new Rate(1.50m);

        var item = new OrderEstimateRequestItem(skuCode, 5, 2.5m, unitPickFee);

        Assert.Multiple(() =>
        {
            Assert.That(item.SkuCode, Is.EqualTo(skuCode));
            Assert.That(item.TotalQuantity, Is.EqualTo(5));
            Assert.That(item.UnitWeight, Is.EqualTo(2.5m));
            Assert.That(item.UnitPickFee, Is.EqualTo(unitPickFee));
        });
    }

    #endregion
}
