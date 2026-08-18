using Throughline.Common.Results;
using Throughline.Modules.Ordering.Application.Orders;
using Throughline.Modules.Ordering.Application.Orders.Models;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Pricing;
using Throughline.Modules.Ordering.Domain.Skus;

namespace Throughline.Modules.Ordering.Application.Tests.Orders;

[Category("Unit")]
public sealed class OrderEstimateRequestBuilderTests
{
    private const int MerchantId = 42;

    private static readonly Rate HandlingRate = new(0.25m);

    private static readonly MerchantPickFee Sku1PickFee = new(new SkuCode("SKU-1"), new Rate(1.50m), 1);
    private static readonly MerchantPickFee Sku2PickFee = new(new SkuCode("SKU-2"), new Rate(2.00m), 1);

    private static readonly SkuAttributes Sku1Attributes = new(new SkuCode("SKU-1"), 0.5m);
    private static readonly SkuAttributes Sku2Attributes = new(new SkuCode("SKU-2"), 1.25m);

    private static readonly ZoneSurcharge Zone = new(
        MerchantId, new PostalZone(new PostalCode("10000"), new PostalCode("19999")), new Money(5m));

    private static readonly ZoneSurcharge OtherZone = new(
        MerchantId, new PostalZone(new PostalCode("20000"), new PostalCode("29999")), new Money(9m));

    private Mock<IMerchantRateQuery> _merchantRateQuery = null!;
    private Mock<IPickFeeQuery> _pickFeeQuery = null!;

    private Mock<ISkuAttributesQuery> _skuAttributesQuery = null!;
    private OrderEstimateRequestBuilder _sut = null!;
    private Mock<IZoneChargeQuery> _zoneChargeQuery = null!;

    [SetUp]
    public void SetUp()
    {
        _skuAttributesQuery = new Mock<ISkuAttributesQuery>(MockBehavior.Strict);
        _zoneChargeQuery = new Mock<IZoneChargeQuery>(MockBehavior.Strict);
        _pickFeeQuery = new Mock<IPickFeeQuery>(MockBehavior.Strict);
        _merchantRateQuery = new Mock<IMerchantRateQuery>(MockBehavior.Strict);

        _sut = new OrderEstimateRequestBuilder(
            _skuAttributesQuery.Object,
            _zoneChargeQuery.Object,
            _pickFeeQuery.Object,
            _merchantRateQuery.Object);
    }

    private void GivenPickFees(params MerchantPickFee[] pickFees)
    {
        _pickFeeQuery.Setup(q => q.GetPickFeesAsync(MerchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pickFees);
    }

    private void GivenSkuAttributes(params SkuAttributes[] attributes)
    {
        _skuAttributesQuery
            .Setup(q => q.GetAttributesAsync(
                MerchantId, It.IsAny<IEnumerable<SkuCode>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attributes);
    }

    private void GivenZones(params ZoneSurcharge[] zones)
    {
        _zoneChargeQuery.Setup(q => q.GetChargesAsync(MerchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zones);
    }

    private void GivenHandlingRate(Rate? handlingRate)
    {
        _merchantRateQuery.Setup(q => q.GetHandlingAsync(MerchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlingRate);
    }

    private static CreateOrderCommand Command(params (string Sku, int Quantity)[] items)
    {
        return CreateOrderCommand.Create(
            MerchantId, "PO-1", "1 Main St", "Apt 2", "Springfield", "IL", "10000",
            items, "REF-1").Value!;
    }

    private static void AssertUnavailable(Result<OrderEstimateRequest> result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Errors.Single().ErrorType, Is.EqualTo(ErrorType.Unavailable));
        });
    }

    #region CreateRequestAsync

    [Test]
    public async Task CreateRequestAsync_AllReferenceDataPresent_ReturnsPopulatedRequest()
    {
        GivenPickFees(Sku1PickFee, Sku2PickFee);
        GivenSkuAttributes(Sku1Attributes, Sku2Attributes);
        GivenZones(Zone);
        GivenHandlingRate(HandlingRate);

        var result = await _sut.CreateRequestAsync(Command(("SKU-1", 2), ("SKU-2", 4)));

        Assert.That(result.Succeeded, Is.True);
        var request = result.Value!;
        var sku1 = request.Items.Single(i => i.SkuCode.Value == "SKU-1");
        var sku2 = request.Items.Single(i => i.SkuCode.Value == "SKU-2");
        Assert.Multiple(() =>
        {
            Assert.That(request.Items.Count(), Is.EqualTo(2));
            Assert.That(request.HandlingRate, Is.EqualTo(HandlingRate));
            Assert.That(request.ZoneCharge, Is.SameAs(Zone));

            Assert.That(sku1.TotalQuantity, Is.EqualTo(2));
            Assert.That(sku1.UnitWeight, Is.EqualTo(0.5m));
            Assert.That(sku1.UnitPickFee, Is.EqualTo(new Rate(1.50m)));

            Assert.That(sku2.TotalQuantity, Is.EqualTo(4));
            Assert.That(sku2.UnitWeight, Is.EqualTo(1.25m));
            Assert.That(sku2.UnitPickFee, Is.EqualTo(new Rate(2.00m)));
        });
    }

    [Test]
    public async Task CreateRequestAsync_DuplicateSkus_SumsQuantitiesIntoOneLine()
    {
        GivenPickFees(Sku1PickFee, Sku2PickFee);
        GivenSkuAttributes(Sku1Attributes, Sku2Attributes);
        GivenZones(Zone);
        GivenHandlingRate(HandlingRate);

        var result = await _sut.CreateRequestAsync(Command(("SKU-1", 2), ("SKU-2", 4), ("SKU-1", 3)));

        Assert.That(result.Succeeded, Is.True);
        var request = result.Value!;
        Assert.Multiple(() =>
        {
            Assert.That(request.Items.Count(), Is.EqualTo(2));
            Assert.That(request.Items.Single(i => i.SkuCode.Value == "SKU-1").TotalQuantity, Is.EqualTo(5));
            Assert.That(request.Items.Single(i => i.SkuCode.Value == "SKU-2").TotalQuantity, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task CreateRequestAsync_MultipleZones_SelectsZoneContainingDestination()
    {
        GivenPickFees(Sku1PickFee, Sku2PickFee);
        GivenSkuAttributes(Sku1Attributes, Sku2Attributes);
        GivenZones(OtherZone, Zone);
        GivenHandlingRate(HandlingRate);

        var result = await _sut.CreateRequestAsync(Command(("SKU-1", 2), ("SKU-2", 4)));

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Value!.ZoneCharge, Is.SameAs(Zone));
    }

    [Test]
    public async Task CreateRequestAsync_SubmittedSkuHasNoPickFee_ReturnsUnavailable()
    {
        GivenPickFees(Sku1PickFee);
        GivenSkuAttributes(Sku1Attributes, Sku2Attributes);
        GivenZones(Zone);
        GivenHandlingRate(HandlingRate);

        var result = await _sut.CreateRequestAsync(Command(("SKU-1", 2), ("SKU-2", 4)));

        AssertUnavailable(result);
    }

    [Test]
    public async Task CreateRequestAsync_SubmittedSkuHasNoAttributes_ReturnsUnavailable()
    {
        GivenPickFees(Sku1PickFee, Sku2PickFee);
        GivenSkuAttributes(Sku1Attributes);
        GivenZones(Zone);
        GivenHandlingRate(HandlingRate);

        var result = await _sut.CreateRequestAsync(Command(("SKU-1", 2), ("SKU-2", 4)));

        AssertUnavailable(result);
    }

    [Test]
    public async Task CreateRequestAsync_DestinationOutsideAllZones_ReturnsUnavailable()
    {
        GivenPickFees(Sku1PickFee, Sku2PickFee);
        GivenSkuAttributes(Sku1Attributes, Sku2Attributes);
        GivenZones(OtherZone);
        GivenHandlingRate(HandlingRate);

        var result = await _sut.CreateRequestAsync(Command(("SKU-1", 2), ("SKU-2", 4)));

        AssertUnavailable(result);
    }

    [Test]
    public async Task CreateRequestAsync_MerchantHasNoHandlingRate_ReturnsUnavailable()
    {
        GivenPickFees(Sku1PickFee, Sku2PickFee);
        GivenSkuAttributes(Sku1Attributes, Sku2Attributes);
        GivenZones(Zone);
        GivenHandlingRate(null);

        var result = await _sut.CreateRequestAsync(Command(("SKU-1", 2), ("SKU-2", 4)));

        AssertUnavailable(result);
    }

    #endregion
}