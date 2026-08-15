using Moq;
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

    private static readonly ZoneSurcharge Zone = new(
        MerchantId, new PostalZone(new PostalCode("10000"), new PostalCode("19999")), new Money(5m));

    private static readonly (string Sku, int Quantity)[] DefaultItems = [("SKU-1", 2), ("SKU-2", 4)];

    private Mock<ISkuAttributesQuery> _skuAttributesQuery = null!;
    private Mock<IZoneChargeQuery> _zoneChargeQuery = null!;
    private Mock<IPickFeeQuery> _pickFeeQuery = null!;
    private Mock<IMerchantRateQuery> _merchantRateQuery = null!;
    private OrderEstimateRequestBuilder _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _skuAttributesQuery = new Mock<ISkuAttributesQuery>(MockBehavior.Strict);
        _zoneChargeQuery = new Mock<IZoneChargeQuery>(MockBehavior.Strict);
        _pickFeeQuery = new Mock<IPickFeeQuery>(MockBehavior.Strict);
        _merchantRateQuery = new Mock<IMerchantRateQuery>(MockBehavior.Strict);

        _pickFeeQuery
            .Setup(q => q.GetPickFeesAsync(MerchantId))
            .ReturnsAsync([
                new SkuPickFee(new SkuCode("SKU-1"), new Rate(1.50m)),
                new SkuPickFee(new SkuCode("SKU-2"), new Rate(2.00m))
            ]);

        _skuAttributesQuery
            .Setup(q => q.GetAttributesAsync(MerchantId, It.IsAny<IEnumerable<SkuCode>>()))
            .ReturnsAsync([
                new SkuAttributes(new SkuCode("SKU-1"), 0.5m),
                new SkuAttributes(new SkuCode("SKU-2"), 1.25m)
            ]);

        _zoneChargeQuery
            .Setup(q => q.GetChargesAsync(MerchantId))
            .ReturnsAsync([Zone]);

        _merchantRateQuery
            .Setup(q => q.GetHandlingAsync(MerchantId))
            .ReturnsAsync(HandlingRate);

        _sut = new OrderEstimateRequestBuilder(
            _skuAttributesQuery.Object,
            _zoneChargeQuery.Object,
            _pickFeeQuery.Object,
            _merchantRateQuery.Object);
    }

    #region CreateRequestAsync

    [Test]
    public async Task CreateRequestAsync_AllReferenceDataPresent_ReturnsPopulatedRequest()
    {
        var result = await _sut.CreateRequestAsync(Command());

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
        var command = Command([("SKU-1", 2), ("SKU-2", 4), ("SKU-1", 3)]);

        var result = await _sut.CreateRequestAsync(command);

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
        var otherZone = new ZoneSurcharge(
            MerchantId, new PostalZone(new PostalCode("20000"), new PostalCode("29999")), new Money(9m));
        _zoneChargeQuery
            .Setup(q => q.GetChargesAsync(MerchantId))
            .ReturnsAsync([otherZone, Zone]);

        var result = await _sut.CreateRequestAsync(Command());

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Value!.ZoneCharge, Is.SameAs(Zone));
    }

    [Test]
    public async Task CreateRequestAsync_SubmittedSkuHasNoPickFee_ReturnsUnavailable()
    {
        _pickFeeQuery
            .Setup(q => q.GetPickFeesAsync(MerchantId))
            .ReturnsAsync([new SkuPickFee(new SkuCode("SKU-1"), new Rate(1.50m))]);

        var result = await _sut.CreateRequestAsync(Command());

        AssertUnavailable(result);
    }

    [Test]
    public async Task CreateRequestAsync_SubmittedSkuHasNoAttributes_ReturnsUnavailable()
    {
        _skuAttributesQuery
            .Setup(q => q.GetAttributesAsync(MerchantId, It.IsAny<IEnumerable<SkuCode>>()))
            .ReturnsAsync([new SkuAttributes(new SkuCode("SKU-1"), 0.5m)]);

        var result = await _sut.CreateRequestAsync(Command());

        AssertUnavailable(result);
    }

    [Test]
    public async Task CreateRequestAsync_DestinationOutsideAllZones_ReturnsUnavailable()
    {
        var otherZone = new ZoneSurcharge(
            MerchantId, new PostalZone(new PostalCode("20000"), new PostalCode("29999")), new Money(9m));
        _zoneChargeQuery
            .Setup(q => q.GetChargesAsync(MerchantId))
            .ReturnsAsync([otherZone]);

        var result = await _sut.CreateRequestAsync(Command());

        AssertUnavailable(result);
    }

    [Test]
    public async Task CreateRequestAsync_MerchantHasNoHandlingRate_ReturnsUnavailable()
    {
        _merchantRateQuery
            .Setup(q => q.GetHandlingAsync(MerchantId))
            .ReturnsAsync((Rate?)null);

        var result = await _sut.CreateRequestAsync(Command());

        AssertUnavailable(result);
    }

    #endregion

    private static CreateOrderCommand Command(IEnumerable<(string Sku, int Quantity)>? items = null)
    {
        return CreateOrderCommand.Create(
            MerchantId, "PO-1", "1 Main St", "Apt 2", "Springfield", "IL", "10000", "US",
            items ?? DefaultItems, "REF-1").Value!;
    }

    private static void AssertUnavailable(Result<OrderEstimateRequest> result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Errors.Single().ErorType, Is.EqualTo(ErrorType.Unavailable));
        });
    }
}
