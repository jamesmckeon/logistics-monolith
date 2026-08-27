using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.Tests.Pricing;

[Category("Unit")]
public sealed class ZoneSurchargeTests
{
    private static PostalZone Zone(string start, string end) =>
        new(new PostalCode(start), new PostalCode(end));

    private static Money Fee(decimal value) => new(value);

    #region Constructor

    [Test]
    public void Constructor_NullPostalZone_ThrowsExpected()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _ = new ZoneSurcharge(1, null!, Fee(5m)));

        Assert.That(ex.ParamName, Is.EqualTo("postalZone"));
    }

    [Test]
    public void Constructor_NullSurcharge_ThrowsExpected()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _ = new ZoneSurcharge(1, Zone("05001", "05005"), null!));

        Assert.That(ex.ParamName, Is.EqualTo("surcharge"));
    }

    [Test]
    public void Constructor_ValidArguments_StoresProperties()
    {
        var zone = Zone("05001", "05005");
        var fee = Fee(5m);

        var sut = new ZoneSurcharge(7, zone, fee);

        Assert.Multiple(() =>
        {
            Assert.That(sut.MerchantId, Is.EqualTo(7));
            Assert.That(sut.PostalZone, Is.SameAs(zone));
            Assert.That(sut.Surcharge, Is.SameAs(fee));
        });
    }

    #endregion
}
