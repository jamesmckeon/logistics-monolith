using Throughline.Common.Results;
using Throughline.Modules.Ordering.Domain;
using Throughline.Modules.Ordering.Domain.Orders;

namespace Throughline.Modules.Ordering.Tests.Domain.Orders;

[Category("Unit")]
public sealed class OrderContentTests
{
    private static StreetAddress TestAddress()
    {
        return new StreetAddress("Test Address 1", null, "Portland", "OR", new PostalCode("97211"));
    }

    #region Equality

    [Test]
    public void Equals_DifferentPoNumber_ReturnsFalse()
    {
        var address = TestAddress();
        var lines = new OrderLine[] { new(new SkuCode("Test"), 1) };

        var a = new OrderContent("TestPo1", address, lines);
        var b = new OrderContent("TestPo2", address, lines);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equals_DifferentAddress_ReturnsFalse()
    {
        var addressOne = new StreetAddress("Test Address 1", null, "Portland", "OR", new PostalCode("97211"));
        var addressTwo = new StreetAddress("Test Address 2", null, "Portland", "OR", new PostalCode("97211"));
        var lines = new OrderLine[] { new(new SkuCode("Test"), 1) };

        var a = new OrderContent("TestPo", addressOne, lines);
        var b = new OrderContent("TestPo", addressTwo, lines);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equals_DifferentLines_ReturnsFalse()
    {
        var address = TestAddress();
        var linesOne = new OrderLine[] { new(new SkuCode("Test"), 1) };
        var linesTwo = new OrderLine[] { new(new SkuCode("Test"), 2) };

        var a = new OrderContent("TestPo", address, linesOne);
        var b = new OrderContent("TestPo", address, linesTwo);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equals_SameProperties_ReturnsTrue()
    {
        var po = "TestPo";
        var address = TestAddress();
        var lines = new OrderLine[] { new(new SkuCode("Test"), 1), new(new SkuCode("Test1"), 1) };

        var a = new OrderContent(po, address, lines);
        var b = new OrderContent(po, address, lines.Reverse()); // order shouldn't matter

        Assert.That(a, Is.EqualTo(b));
    }

    #endregion

    #region Create

    [TestCase("")]
    [TestCase(" ")]
    public void Create_MissingPoNumber_ReturnsFailure(string poNumber)
    {
        var result = OrderContent.Create(
            poNumber,
            new StreetAddress("Test Address", null, "Portland", "OR", new PostalCode("97111")),
            [new OrderLine(new("Test"), 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(
                result.Errors.Single().Description,
                Is.EqualTo("purchaseOrderNumber is required"));
        });
    }

    [Test]
    public void Create_DuplicateSku_ReturnsFailure()
    {
        var lines = new OrderLine[]
        {
            new(new SkuCode("Test"), 1),
            new(new SkuCode("Test"), 2)
        };

        var result = OrderContent.Create(
            "TestPo",
            new StreetAddress("Test Address", null, "Portland", "OR", new PostalCode("97111")),
            lines);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(
                result.Errors.Single().Description,
                Is.EqualTo("An order cannot have more than one line with the same sku code"));
        });
    }

    [Test]
    public void Create_EmptyLines_ReturnsFailure()
    {
        var result = OrderContent.Create(
            "TestPo",
            new StreetAddress("Test Address", null, "Portland", "OR", new PostalCode("97111")),
            Array.Empty<OrderLine>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.Validation));
            Assert.That(
                result.Errors.Single().Description,
                Is.EqualTo("An order must have at least one line"));
        });
    }

    [Test]
    public void Create_ValidInput_ReturnsSuccess()
    {
        var lines = new OrderLine[]
        {
            new(new SkuCode("Test"), 1)
        };

        var po = " test po ";
        var address = new StreetAddress("Test Address", null, "Portland", "OR", new PostalCode("97111"));

        var result = OrderContent.Create(
            po, address, lines);

        Assert.That(result.Value, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.PurchaseOrderNumber, Is.EqualTo(po.Trim()));
            Assert.That(result.Value.Destination, Is.EqualTo(address));
            Assert.That(result.Value.OrderLines, Is.EqualTo(lines));
        });
    }

    #endregion
}