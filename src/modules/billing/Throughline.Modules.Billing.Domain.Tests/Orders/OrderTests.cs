using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Billing.Domain.OrderEstimates;
using Throughline.Modules.Billing.Domain.Orders;
using Throughline.Modules.Billing.Domain.Pricing;

namespace Throughline.Modules.Billing.Domain.Tests.Orders;

[Category("Unit")]
public sealed class OrderTests
{
    private const int MerchantId = 7;

    private static readonly OrderId Id = new(Guid.CreateVersion7());

    private static readonly OrderEstimateItem Item1 = new(
        new SkuCode("SKU-1"), 2, new Rate(1.50m), new Money(3.00m), 4m, new Money(0.75m));

    private static readonly OrderEstimateItem Item2 = new(
        new SkuCode("SKU-2"), 5, new Rate(2.00m), new Money(10.00m), 1.5m, new Money(1.25m));

    private static readonly OrderEstimate Estimate = new(
        new Money(5.00m), new Money(20.00m), new Rate(0.25m), [Item1, Item2]);

    private static readonly StreetAddress Destination = new(
        "1 Main St", "Apt 2", "Portland", new AddressState("OR"), new PostalCode("97218"));

    private static Order Create()
    {
        return Order.FromOrderEstimate(Id, Estimate, MerchantId, "PO-1", "REF-1", Destination);
    }

    #region FromOrderEstimate

    [Test]
    public void FromOrderEstimate_NullOrderId_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Order.FromOrderEstimate(null!, Estimate, MerchantId, "PO-1", "REF-1", Destination));
        Assert.That(ex.ParamName, Is.EqualTo("orderId"));
    }

    [Test]
    public void FromOrderEstimate_NullEstimate_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Order.FromOrderEstimate(Id, null!, MerchantId, "PO-1", "REF-1", Destination));
        Assert.That(ex.ParamName, Is.EqualTo("orderEstimate"));
    }

    [Test]
    public void FromOrderEstimate_NullDestination_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Order.FromOrderEstimate(Id, Estimate, MerchantId, "PO-1", "REF-1", null!));
        Assert.That(ex.ParamName, Is.EqualTo("destination"));
    }

    [Test]
    public void FromOrderEstimate_NullPurchaseOrderNumber_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Order.FromOrderEstimate(Id, Estimate, MerchantId, null!, "REF-1", Destination));
        Assert.That(ex.ParamName, Is.EqualTo("purchaseOrderNumber"));
    }

    [Test]
    public void FromOrderEstimate_NullReferenceNumber_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Order.FromOrderEstimate(Id, Estimate, MerchantId, "PO-1", null!, Destination));
        Assert.That(ex.ParamName, Is.EqualTo("referenceNumber"));
    }

    [Test]
    public void FromOrderEstimate_ValidInputs_MapsIdMerchantAndDestination()
    {
        var order = Create();

        Assert.Multiple(() =>
        {
            Assert.That(order.Id, Is.EqualTo(Id));
            Assert.That(order.MerchantId, Is.EqualTo(MerchantId));
            Assert.That(order.Destination, Is.SameAs(Destination));
        });
    }

    [Test]
    public void FromOrderEstimate_ValidInputs_CarriesEstimateSurchargeAndTotal()
    {
        var order = Create();

        Assert.Multiple(() =>
        {
            Assert.That(order.DestinationSurcharge, Is.EqualTo(Estimate.ZoneSurcharge));
            Assert.That(order.TotalCharges, Is.EqualTo(Estimate.TotalCharge));
        });
    }

    [Test]
    public void FromOrderEstimate_PaddedOrderNumbers_TrimsThem()
    {
        var order = Order.FromOrderEstimate(
            Id, Estimate, MerchantId, "  PO-1  ", "  REF-1  ", Destination);

        Assert.Multiple(() =>
        {
            Assert.That(order.PurchaseOrderNumber, Is.EqualTo("PO-1"));
            Assert.That(order.ReferenceNumber, Is.EqualTo("REF-1"));
        });
    }

    [Test]
    public void FromOrderEstimate_ValidInputs_ProducesOneLinePerEstimateItem()
    {
        var order = Create();

        Assert.That(order.OrderLines.Count(), Is.EqualTo(2));
    }

    [Test]
    public void FromOrderEstimate_ValidInputs_MapsEachEstimateItemFieldToLine()
    {
        var order = Create();

        var line = order.OrderLines.First();

        Assert.Multiple(() =>
        {
            Assert.That(line.TotalQuantity, Is.EqualTo(Item1.Quantity));
            Assert.That(line.SkuCode, Is.EqualTo(Item1.SkuCode));
            Assert.That(line.UnitPickFee, Is.EqualTo(Item1.PickFeeRate));
            Assert.That(line.TotalPickFees, Is.EqualTo(Item1.TotalPickFee));
            Assert.That(line.TotalWeight, Is.EqualTo(Item1.Weight));
            Assert.That(line.TotalHandling, Is.EqualTo(Item1.TotalHandling));
        });
    }

    [Test]
    public void FromOrderEstimate_ValidInputs_TotalWeightIsSumOfLineWeights()
    {
        var order = Create();

        // Item1.Weight (4) + Item2.Weight (1.5)
        Assert.That(order.TotalWeight, Is.EqualTo(5.5m));
    }

    [Test]
    public void FromOrderEstimate_EstimateHasNoItems_ProducesNoLinesAndZeroWeight()
    {
        var estimate = new OrderEstimate(new Money(5m), new Money(0m), new Rate(0.25m), []);

        var order = Order.FromOrderEstimate(Id, estimate, MerchantId, "PO-1", "REF-1", Destination);

        Assert.Multiple(() =>
        {
            Assert.That(order.OrderLines, Is.Empty);
            Assert.That(order.TotalWeight, Is.EqualTo(0m));
        });
    }

    #endregion
}
