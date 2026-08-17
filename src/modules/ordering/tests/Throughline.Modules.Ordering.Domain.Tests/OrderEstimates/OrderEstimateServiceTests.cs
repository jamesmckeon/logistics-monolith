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

    private static OrderEstimateRequest Request(
        OrderEstimateRequestItem[] items,
        decimal handlingRate,
        decimal surcharge)
    {
        return new OrderEstimateRequest(new Rate(handlingRate), items, Zone(surcharge));
    }

    private static OrderEstimateRequestItem Item(
        string sku, int totalQuantity, decimal unitWeight, decimal unitPickFee)
    {
        return new OrderEstimateRequestItem(
            new SkuCode(sku), totalQuantity, unitWeight, new Rate(unitPickFee));
    }

    private static ZoneSurcharge Zone(decimal surcharge)
    {
        return new ZoneSurcharge(
            1, new PostalZone(new PostalCode("10000"), new PostalCode("19999")), new Money(surcharge));
    }

    #region GetEstimate

    [Test]
    public void GetEstimate_SingleLine_TotalChargeIsPickFeePlusHandlingPlusSurcharge()
    {
        // pick fee: 3 * 1.50 = 4.50; handling: (3 * 2) * 0.25 = 1.50; surcharge: 5.00 => 11.00
        var request = Request([Item("SKU-1", 3, 2m, 1.50m)], 0.25m, 5.00m);
        var estimate = _sut.GetEstimate(request);

        Assert.That(estimate.TotalCharge.Value, Is.EqualTo(11.00m));
    }

    [Test]
    public void GetEstimate_SingleLine_LineCarriesComputedPickFeeWeightAndHandling()
    {
        var request = Request([Item("SKU-1", 3, 2m, 1.50m)], 0.25m, 5.00m);

        var line = _sut.GetEstimate(request).Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(line.Quantity, Is.EqualTo(3));
            Assert.That(line.TotalPickFee.Value, Is.EqualTo(4.50m));
            Assert.That(line.Weight, Is.EqualTo(6m));
            Assert.That(line.TotalHandling.Value, Is.EqualTo(1.50m));
        });
    }

    [Test]
    public void GetEstimate_ValidRequest_CarriesZoneSurcharge()
    {
        var request = Request([Item("SKU-1", 1, 1m, 1.00m)], 0.10m, 5.00m);

        var estimate = _sut.GetEstimate(request);

        Assert.That(estimate.ZoneSurcharge.Value, Is.EqualTo(5.00m));
    }

    [Test]
    public void GetEstimate_ValidRequest_CarriesRequestHandlingRate()
    {
        var handlingRate = new Rate(0.25m);
        var request = new OrderEstimateRequest(
            handlingRate, [Item("SKU-1", 1, 1m, 1.00m)], Zone(5.00m));

        var estimate = _sut.GetEstimate(request);

        Assert.That(estimate.HandlingRate, Is.EqualTo(handlingRate));
    }

    [Test]
    public void GetEstimate_MultipleLines_ProducesOneEstimateItemPerLineAndSumsCharges()
    {
        // SKU-1: pick 2 * 1.00 = 2.00, handling (2 * 1) * 0.10 = 0.20
        // SKU-2: pick 3 * 1.00 = 3.00, handling (3 * 1) * 0.10 = 0.30
        // surcharge 0.00 => total 5.50
        var request = Request(
            [
                Item("SKU-1", 2, 1m, 1.00m),
                Item("SKU-2", 3, 1m, 1.00m)
            ],
            0.10m,
            0.00m);

        var estimate = _sut.GetEstimate(request);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.Items.Count(), Is.EqualTo(2));
            Assert.That(estimate.TotalCharge.Value, Is.EqualTo(5.50m));
        });
    }

    #endregion
}