using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.Tests.OrderEstimates;

[Category("Unit")]
public sealed class OrderEstimateServiceTests
{
    private OrderEstimateService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new OrderEstimateService();
    }

    #region GetEstimate

    [Test]
    public void GetEstimate_ValidSingleLineRequest_Succeeds()
    {
        var request = Request(
            items: [Item("SKU-1", quantity: 3, weight: 2m)],
            pickFees: [PickFee("SKU-1", 1.50m)],
            zones: [Zone("10000", "19999", surcharge: 5.00m)],
            handlingRate: new Rate(0.25m),
            destination: Zip("10001"));

        var result = _sut.GetEstimate(request);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void GetEstimate_ValidSingleLine_TotalChargeIsPickFeePlusHandlingPlusSurcharge()
    {
        // pick fee: 3 * 1.50 = 4.50; handling: (3 * 2) * 0.25 = 1.50; surcharge: 5.00 => 11.00
        var request = Request(
            items: [Item("SKU-1", quantity: 3, weight: 2m)],
            pickFees: [PickFee("SKU-1", 1.50m)],
            zones: [Zone("10000", "19999", surcharge: 5.00m)],
            handlingRate: new Rate(0.25m),
            destination: Zip("10001"));

        var result = _sut.GetEstimate(request);

        Assert.That(result.Value!.TotalCharge.Value, Is.EqualTo(11.00m));
    }

    [Test]
    public void GetEstimate_ValidSingleLine_LineCarriesPickFeeAndHandlingCharges()
    {
        var request = Request(
            items: [Item("SKU-1", quantity: 3, weight: 2m)],
            pickFees: [PickFee("SKU-1", 1.50m)],
            zones: [Zone("10000", "19999", surcharge: 5.00m)],
            handlingRate: new Rate(0.25m),
            destination: Zip("10001"));

        var line = _sut.GetEstimate(request).Value!.Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(line.Quantity, Is.EqualTo(3));
            Assert.That(line.TotalPickFee.Value, Is.EqualTo(4.50m));
            Assert.That(line.TotalHandling.Value, Is.EqualTo(1.50m));
        });
    }

    [Test]
    public void GetEstimate_ValidRequest_CarriesSurchargeOfTheZoneCoveringDestination()
    {
        var request = Request(
            items: [Item("SKU-1", quantity: 1, weight: 1m)],
            pickFees: [PickFee("SKU-1", 1.00m)],
            zones:
            [
                Zone("10000", "19999", surcharge: 5.00m),
                Zone("20000", "29999", surcharge: 99.00m)
            ],
            handlingRate: new Rate(0.10m),
            destination: Zip("10001"));

        var estimate = _sut.GetEstimate(request).Value!;

        Assert.That(estimate.ZoneSurcharge.Value, Is.EqualTo(5.00m));
    }

    [Test]
    public void GetEstimate_MultipleLinesShareSku_SumsQuantitiesIntoOneLine()
    {
        var request = Request(
            items:
            [
                Item("SKU-1", quantity: 2, weight: 1m),
                Item("SKU-1", quantity: 3, weight: 1m)
            ],
            pickFees: [PickFee("SKU-1", 1.00m)],
            zones: [Zone("10000", "19999", surcharge: 0.00m)],
            handlingRate: new Rate(0.10m),
            destination: Zip("10001"));

        var estimate = _sut.GetEstimate(request).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(estimate.Items.Count(), Is.EqualTo(1));
            Assert.That(estimate.Items.Single().Quantity, Is.EqualTo(5));
        });
    }

    [Test]
    public void GetEstimate_ValidRequest_CarriesRequestHandlingRate()
    {
        var handlingRate = new Rate(0.25m);
        var request = Request(
            items: [Item("SKU-1", quantity: 1, weight: 1m)],
            pickFees: [PickFee("SKU-1", 1.00m)],
            zones: [Zone("10000", "19999", surcharge: 5.00m)],
            handlingRate: handlingRate,
            destination: Zip("10001"));

        var estimate = _sut.GetEstimate(request).Value!;

        Assert.That(estimate.HandlingRate, Is.EqualTo(handlingRate));
    }

    #endregion

    private static OrderEstimateRequest Request(
        OrderEstimateRequestItem[] items,
        (CaseInsensitiveString Sku, Rate PickFee)[] pickFees,
        ZoneSurcharge[] zones,
        Rate handlingRate,
        PostalCode destination)
    {
        return new OrderEstimateRequest(destination, handlingRate, items, zones, pickFees);
    }

    private static OrderEstimateRequestItem Item(string sku, int quantity, decimal weight)
    {
        return new OrderEstimateRequestItem(Sku(sku), quantity, weight);
    }

    private static (CaseInsensitiveString Sku, Rate PickFee) PickFee(string sku, decimal rate)
    {
        return (Sku(sku), new Rate(rate));
    }

    private static ZoneSurcharge Zone(string start, string end, decimal surcharge)
    {
        return new ZoneSurcharge(1, new PostalZone(Zip(start), Zip(end)), new Money(surcharge));
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
