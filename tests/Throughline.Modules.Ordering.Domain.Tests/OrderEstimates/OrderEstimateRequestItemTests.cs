using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;

namespace Throughline.Modules.Ordering.Domain.Tests.OrderEstimates;

[Category("Unit")]
public sealed class OrderEstimateRequestItemTests
{
    #region Constructor

    [TestCase(0)]
    [TestCase(-1)]
    public void Constructor_QuantityAtOrBelowZero_ThrowsArgumentOutOfRange(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OrderEstimateRequestItem(new CaseInsensitiveString("SKU-1"), quantity, 1m));
    }

    [TestCase("0")]
    [TestCase("-0.01")]
    public void Constructor_WeightAtOrBelowZero_ThrowsArgumentOutOfRange(decimal weight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OrderEstimateRequestItem(new CaseInsensitiveString("SKU-1"), 1, weight));
    }

    [Test]
    public void Constructor_NullSkuCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OrderEstimateRequestItem(null!, 1, 1m));
    }

    [Test]
    public void Constructor_ValidArguments_ExposesSuppliedValues()
    {
        var skuCode = new CaseInsensitiveString("SKU-1");

        var item = new OrderEstimateRequestItem(skuCode, 5, 2.5m);

        Assert.Multiple(() =>
        {
            Assert.That(item.SkuCode, Is.EqualTo(skuCode));
            Assert.That(item.Quantity, Is.EqualTo(5));
            Assert.That(item.Weight, Is.EqualTo(2.5m));
        });
    }

    #endregion
}
