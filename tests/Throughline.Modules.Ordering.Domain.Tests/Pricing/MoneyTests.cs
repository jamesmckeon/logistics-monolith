using Throughline.Modules.Ordering.Domain.Models;
using Throughline.Modules.Ordering.Domain.Pricing;

namespace Throughline.Modules.Ordering.Domain.Tests.Pricing;

[Category("Unit")]
public sealed class MoneyTests
{
    #region FromRate

    [TestCase(1, "1.23")]
    [TestCase(2, "45.6789")]
    public void FromRate_Integer_ReturnsExpected(int multiplier, decimal rate)
    {
        var expected = Math.Round(multiplier * rate, 2, MidpointRounding.ToEven);
        var actual = Money.FromRate(multiplier, new Rate(rate));

        Assert.That(actual.Value, Is.EqualTo(expected));
    }

    [TestCase("1.23", "1.23")]
    [TestCase("2.3456789", "45.6789")]
    public void FromRate_Decimal_ReturnsExpected(decimal multiplier, decimal rate)
    {
        var expected = Math.Round(multiplier * rate, 2, MidpointRounding.ToEven);
        var actual = Money.FromRate(multiplier, new Rate(rate));

        Assert.That(actual.Value, Is.EqualTo(expected));
    }

    #endregion

    [TestCase("1.234", "1.23")]
    [TestCase("1.235", "1.24")]
    [TestCase("1.225", "1.22")]
    [TestCase("1.255", "1.26")]
    public void Constructor_ValueWithMoreThanTwoDecimalPlaces_RoundsHalfToEven(decimal value, decimal expected)
    {
        var actual = new Money(value);

        Assert.That(actual.Value, Is.EqualTo(expected));
    }

    [TestCase("1.23", "1.23")]
    [TestCase("0.00", "0.00")]
    [TestCase("-4.50", "-4.50")]
    public void Constructor_ValueWithinTwoDecimalPlaces_IsStoredUnchanged(decimal value, decimal expected)
    {
        var actual = new Money(value);

        Assert.That(actual.Value, Is.EqualTo(expected));
    }

    [TestCase("1.50", "2.25", "3.75")]
    [TestCase("0.10", "0.20", "0.30")]
    [TestCase("5.00", "-2.50", "2.50")]
    public void OperatorPlus_TwoAmounts_ReturnsSum(decimal left, decimal right, decimal expected)
    {
        var actual = new Money(left) + new Money(right);

        Assert.That(actual.Value, Is.EqualTo(expected));
    }

    #region Equals

    [Test]
    public void Equals_SameValue_ReturnsTrue()
    {
        var money = new Money(12.34m);
        var other = new Money(12.34m);

        Assert.Multiple(() =>
        {
            Assert.That(money.Equals(other), Is.True);
            Assert.That(money == other, Is.True);
            Assert.That(money.GetHashCode(), Is.EqualTo(other.GetHashCode()));
        });
    }

    [Test]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        var money = new Money(12.34m);
        var other = new Money(56.78m);

        Assert.Multiple(() =>
        {
            Assert.That(money.Equals(other), Is.False);
            Assert.That(money == other, Is.False);
            Assert.That(money != other, Is.True);
        });
    }

    [Test]
    public void Equals_ValuesEqualOnlyAfterRounding_ReturnsTrue()
    {
        var money = new Money(1.995m);
        var other = new Money(2.00m);

        Assert.That(money.Equals(other), Is.True);
    }

    #endregion
}