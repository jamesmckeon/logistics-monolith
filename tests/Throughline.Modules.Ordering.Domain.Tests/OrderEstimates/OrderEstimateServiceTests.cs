using Throughline.Common.Results;
using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.OrderEstimates;
using Throughline.Modules.Ordering.Domain.Pricing;
using Throughline.Modules.Ordering.Domain.Skus;

namespace Throughline.Modules.Ordering.Domain.Tests.OrderEstimates;

[Category("Unit")]
public sealed class OrderEstimateServiceTests
{
    private OrderEstimateService _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new OrderEstimateService();
    }

    [Test]
    public void GetEstimate_NullRequest_ThrowsExpected()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => _sut.GetEstimate(null!));
        Assert.That(ex.ParamName, Is.EqualTo("request"));
    }

    [Test]
    public void GetEstimate_SurchargeNotFound_ReturnsFailure()
    {
        var items = new OrderEstimateRequestItem[]
        {
            new(new CaseInsensitiveString("TESTSKU"), 1)
        };

        IEnumerable<ZoneSurcharge> zoneCharges =
        [
            new(1, new PostalZone(new PostalCode("97211"), new PostalCode("97211")), new Money(1))
        ];

        IEnumerable<(MerchantSkuCode, Rate)> pickFees =
        [
            (new MerchantSkuCode(1, new CaseInsensitiveString("TESTSKU")), new Rate(1))
        ];

        var request = TestRequest("97212", 1, items, zoneCharges, pickFees);
        var actual = _sut.GetEstimate(request);
        var error = actual.Errors.Single();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Success, Is.False);
            Assert.That(error.ErorType, Is.EqualTo(ErrorType.Unexpected));
            Assert.That(error.Description, Is.EqualTo("Unable to locate a surcharge for postal code 97212"));
        });
    }

    [Test]
    public void GetEstimate_PickFeeNotFound_ReturnsFailure()
    {
        IEnumerable<(MerchantSkuCode, int)> items =
        [
            (new MerchantSkuCode(1, "TESTSKU"), 1)
        ];

        IEnumerable<ZoneSurcharge> zoneCharges =
        [
            new(1, new PostalZone(new PostalCode("97211"), new PostalCode("97211")), new Money(1))
        ];

        IEnumerable<(MerchantSkuCode, Rate)> pickFees =
        [
            (new MerchantSkuCode(1, "TESTSKU1"), new Rate(1))
        ];

        var request = TestRequest("97211", 1, items, zoneCharges, pickFees);
        var actual = _sut.GetEstimate(request);
        var error = actual.Errors.Single();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Success, Is.False);
            Assert.That(error.ErorType, Is.EqualTo(ErrorType.Unexpected));
            Assert.That(error.Description,
                Is.EqualTo("Unable to locate a pick fee for merchant ID 1, Sku 'TESTSKU'"));
        });
    }

    #region Helpers

    private static OrderEstimateRequest TestRequest(
        string destinationCode,
        decimal handlingRate,
        IEnumerable<OrderEstimateRequestItem> items,
        IEnumerable<ZoneSurcharge> zoneCharges,
        IEnumerable<(MerchantSkuCode, Rate)> pickFees)
    {
        return new OrderEstimateRequest(
            new PostalCode(destinationCode),
            new Rate(handlingRate),
            items,
            zoneCharges,
            pickFees);
    }

    private static OrderEstimateRequest TestRequest()
    {
        var destinationCode = new PostalCode("97211");
        var handlingRate = new Rate(1.11m);

        var items = new OrderEstimateRequestItem[]
        {
            new(new CaseInsensitiveString("TESTSKU"), 1),
            new(new CaseInsensitiveString("TESTSKU"), 2),
            new(new CaseInsensitiveString("TESTSKU1"), 2)
        };

        var charges = new ZoneSurcharge[]
        {
            new(
                1,
                new PostalZone(new PostalCode("97211"), new PostalCode("97212")),
                new Money(3.45m))
        };
        
        var fees = new (MerchantSkuCode, Rate)[]
        {
            (items.First().SkuCode, new (12.345m)),
            (items.Last().SkuCode, new (6.789m))
        }
    }

    #endregion
}