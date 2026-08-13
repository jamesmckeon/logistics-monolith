using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.Tests.OrderEstimates;

[Category("Unit")]
public sealed class OrderEstimateRequestTests
{
    private static OrderEstimateRequestItem ValidItem()
    {
        return new OrderEstimateRequestItem(new SkuCode("SKU-1"), 1, 1m, new Rate(1m));
    }

    private static ZoneSurcharge ValidZone()
    {
        return new ZoneSurcharge(
            1, new PostalZone(new PostalCode("10000"), new PostalCode("19999")), new Money(5m));
    }

    #region Constructor

    [Test]
    public void Constructor_NullHandlingRate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OrderEstimateRequest(null!, [ValidItem()], ValidZone()));
    }

    [Test]
    public void Constructor_NullItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OrderEstimateRequest(new Rate(0.25m), null!, ValidZone()));
    }

    [Test]
    public void Constructor_EmptyItems_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrderEstimateRequest(new Rate(0.25m), Array.Empty<OrderEstimateRequestItem>(), ValidZone()));
    }

    [Test]
    public void Constructor_NullZoneCharge_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OrderEstimateRequest(new Rate(0.25m), [ValidItem()], null!));
    }

    [Test]
    public void Constructor_ValidArguments_ExposesSuppliedValues()
    {
        var handlingRate = new Rate(0.25m);
        var item = ValidItem();
        var zone = ValidZone();

        var request = new OrderEstimateRequest(handlingRate, [item], zone);

        Assert.Multiple(() =>
        {
            Assert.That(request.HandlingRate, Is.EqualTo(handlingRate));
            Assert.That(request.Items, Is.EqualTo(new[] { item }));
            Assert.That(request.ZoneCharge, Is.EqualTo(zone));
        });
    }

    #endregion
}
