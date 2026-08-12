using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.Tests.OrderEstimates;

[Category("Unit")]
public sealed class OrderEstimateRequestTests
{
    #region Constructor

    [Test]
    public void Constructor_EmptyItems_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new OrderEstimateRequest(
            Zip("10001"),
            new Rate(0.25m),
            Array.Empty<OrderEstimateRequestItem>(),
            [ValidZone()],
            [ValidPickFee()]));
    }

    [Test]
    public void Constructor_EmptyZoneCharges_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new OrderEstimateRequest(
            Zip("10001"),
            new Rate(0.25m),
            [ValidItem()],
            Array.Empty<ZoneSurcharge>(),
            [ValidPickFee()]));
    }

    [Test]
    public void Constructor_EmptyPickFees_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new OrderEstimateRequest(
            Zip("10001"),
            new Rate(0.25m),
            [ValidItem()],
            [ValidZone()],
            Array.Empty<(CaseInsensitiveString, Rate)>()));
    }

    [Test]
    public void Constructor_NullItems_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderEstimateRequest(
            Zip("10001"),
            new Rate(0.25m),
            null!,
            [ValidZone()],
            [ValidPickFee()]));
    }

    [Test]
    public void Constructor_NullZoneCharges_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderEstimateRequest(
            Zip("10001"),
            new Rate(0.25m),
            [ValidItem()],
            null!,
            [ValidPickFee()]));
    }

    [Test]
    public void Constructor_NullPickFees_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderEstimateRequest(
            Zip("10001"),
            new Rate(0.25m),
            [ValidItem()],
            [ValidZone()],
            null!));
    }

    [Test]
    public void Constructor_NullDestinationCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderEstimateRequest(
            null!,
            new Rate(0.25m),
            [ValidItem()],
            [ValidZone()],
            [ValidPickFee()]));
    }

    [Test]
    public void Constructor_NullHandlingRate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderEstimateRequest(
            Zip("10001"),
            null!,
            [ValidItem()],
            [ValidZone()],
            [ValidPickFee()]));
    }

    [Test]
    public void Constructor_ValidArguments_ExposesSuppliedValues()
    {
        var destination = Zip("10001");
        var handlingRate = new Rate(0.25m);
        var item = ValidItem();
        var zone = ValidZone();
        var pickFee = ValidPickFee();

        var request = new OrderEstimateRequest(
            destination, handlingRate, [item], [zone], [pickFee]);

        Assert.Multiple(() =>
        {
            Assert.That(request.DestinationCode, Is.EqualTo(destination));
            Assert.That(request.HandlingRate, Is.EqualTo(handlingRate));
            Assert.That(request.Items, Is.EqualTo(new[] { item }));
            Assert.That(request.ZoneCharges, Is.EqualTo(new[] { zone }));
            Assert.That(request.PickFees, Is.EqualTo(new[] { pickFee }));
        });
    }

    #endregion

    private static OrderEstimateRequestItem ValidItem()
    {
        return new OrderEstimateRequestItem(Sku("SKU-1"), 1, 1m);
    }

    private static ZoneSurcharge ValidZone()
    {
        return new ZoneSurcharge(1, new PostalZone(Zip("10000"), Zip("19999")), new Money(5m));
    }

    private static (CaseInsensitiveString Sku, Rate PickFee) ValidPickFee()
    {
        return (Sku("SKU-1"), new Rate(1m));
    }

    private static CaseInsensitiveString Sku(string value)
    {
        return new CaseInsensitiveString(value);
    }

    private static PostalCode Zip(string value)
    {
        return new PostalCode(value);
    }
}
