using Throughline.Modules.Billing.Domain.Models;
using Throughline.Modules.Ordering.Domain;

namespace Throughline.Modules.Ordering.Tests.Domain;

[Category("Unit")]
public sealed class StreetAddressTests
{
    private static StreetAddress Given(
        string addressOne = "1 Main St",
        string? addressTwo = "Apt 2",
        string city = "Boston",
        string state = "MA",
        string zip = "05001")
    {
        return new StreetAddress(
            addressOne,
            addressTwo,
            city,
            state,
            new PostalCode(zip));
    }

    #region Equals

    [Test]
    public void Equals_AllFieldsMatch_ReturnsTrue()
    {
        var left = Given();
        var right = Given();

        Assert.Multiple(() =>
        {
            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        });
    }

    [Test]
    public void Equals_NullAndEmptyAddressTwo_ReturnsTrue()
    {
        var left = Given(addressTwo: null);
        var right = Given(addressTwo: "");

        Assert.Multiple(() =>
        {
            Assert.That(left.Equals(right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        });
    }

    [Test]
    public void GetAtomicValues_NullAddressTwo_DoesNotThrow()
    {
        var sut = Given(addressTwo: null);

        Assert.DoesNotThrow(() => _ = sut.GetHashCode());
    }

    [Test]
    public void Equals_DifferentAddressOne_ReturnsFalse()
    {
        var left = Given("1 Main St");
        var right = Given("2 Main St");

        Assert.Multiple(() =>
        {
            Assert.That(left.Equals(right), Is.False);
            Assert.That(left != right, Is.True);
        });
    }

    [Test]
    public void Equals_DifferentAddressTwo_ReturnsFalse()
    {
        var left = Given(addressTwo: "Apt 2");
        var right = Given(addressTwo: "Apt 3");

        Assert.That(left.Equals(right), Is.False);
    }

    [Test]
    public void Equals_DifferentCity_ReturnsFalse()
    {
        var left = Given(city: "Boston");
        var right = Given(city: "Newton");

        Assert.That(left.Equals(right), Is.False);
    }

    [Test]
    public void Equals_DifferentState_ReturnsFalse()
    {
        var left = Given(state: "MA");
        var right = Given(state: "NY");

        Assert.That(left.Equals(right), Is.False);
    }

    [Test]
    public void Equals_DifferentZip_ReturnsFalse()
    {
        var left = Given(zip: "05001");
        var right = Given(zip: "05002");

        Assert.That(left.Equals(right), Is.False);
    }

    #endregion
}