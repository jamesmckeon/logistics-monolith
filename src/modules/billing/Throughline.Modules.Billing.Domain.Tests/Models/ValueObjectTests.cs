using Throughline.Modules.Billing.Domain.Models;

namespace Throughline.Modules.Billing.Domain.Tests.Models;

[Category("Unit")]
public sealed class ValueObjectTests
{
    [Test]
    public void GetHashCode_ReturnsExpected()
    {
        var atomicValues = new object?[]
        {
            "sTring", null, 1
        };


        var expected = atomicValues.Aggregate(
            0,
            (hashcode, value) =>
                HashCode.Combine(hashcode, value?.GetHashCode()));

        var sut = new Mock(atomicValues);
        var actual = sut.GetHashCode();

        Assert.That(actual, Is.EqualTo(expected));
    }

    private sealed class Mock(params object?[] values) : ValueObject
    {
        protected override IEnumerable<object?> GetAtomicValues()
        {
            return values;
        }
    }

    #region Equals

    [Test]
    public void Equals_SameValues_ReturnsTrue()
    {
        var left = new Mock("a", 1);
        var right = new Mock("a", 1);
        Assert.That(left.Equals(right), Is.True);
    }

    [Test]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var left = new Mock("a", 1);
        var right = new Mock("A", 1);
        Assert.That(left.Equals(right), Is.False);
    }

    [Test]
    public void Equals_NullValue_ReturnsTrue()
    {
        var left = new Mock(null!, 1);
        var right = new Mock(null!, 1);
        Assert.That(left.Equals(right), Is.True);
    }

    [Test]
    public void Equals_NullValue_ReturnsFalse()
    {
        var left = new Mock(null!, 1);
        var right = new Mock("A", 1);
        Assert.That(left.Equals(right), Is.False);
    }

    [Test]
    public void EqualsOverload_SameValues_ReturnsTrue()
    {
        var left = new Mock("a", 1);
        object right = new Mock("a", 1);
        Assert.That(left.Equals(right), Is.True);
    }

    [Test]
    public void EqualsOverload_DifferentValues_ReturnsFalse()
    {
        var left = new Mock("a", 1);
        object right = new Mock("A", 1);
        Assert.That(left.Equals(right), Is.False);
    }

    [Test]
    public void EqualsOverload_NullValue_ReturnsTrue()
    {
        var left = new Mock(null!, 1);
        object right = new Mock(null!, 1);
        Assert.That(left.Equals(right), Is.True);
    }

    [Test]
    public void EqualsOverload_NullValue_ReturnsFalse()
    {
        var left = new Mock(null!, 1);
        object right = new Mock("A", 1);
        Assert.That(left.Equals(right), Is.False);
    }

    #endregion

    #region Equality Operators

    [Test]
    public void EqualityOperator_SameValues_ReturnsTrue()
    {
        var left = new Mock("a", 1);
        var right = new Mock("a", 1);
        Assert.That(left == right, Is.True);
    }

    [Test]
    public void EqualityOperator_DifferentValues_ReturnsFalse()
    {
        var left = new Mock("a", 1);
        var right = new Mock("A", 1);
        Assert.That(left == right, Is.False);
    }

    [Test]
    public void EqualityOperator_NullValue_ReturnsTrue()
    {
        var left = new Mock(null!, 1);
        var right = new Mock(null!, 1);
        Assert.That(left == right, Is.True);
    }

    [Test]
    public void EqualityOperator_NullValue_ReturnsFalse()
    {
        var left = new Mock(null!, 1);
        var right = new Mock("A", 1);
        Assert.That(left == right, Is.False);
    }

    [Test]
    public void InEqualityOperator_SameValues_ReturnsFalse()
    {
        var left = new Mock("a", 1);
        var right = new Mock("a", 1);
        Assert.That(left != right, Is.False);
    }

    [Test]
    public void InEqualityOperator_DifferentValues_ReturnsTrue()
    {
        var left = new Mock("a", 1);
        var right = new Mock("A", 1);
        Assert.That(left != right, Is.True);
    }

    [Test]
    public void InEqualityOperator_NullValue_ReturnsFalse()
    {
        var left = new Mock(null!, 1);
        var right = new Mock(null!, 1);
        Assert.That(left != right, Is.False);
    }

    [Test]
    public void InEqualityOperator_NullValue_ReturnsTrue()
    {
        var left = new Mock(null!, 1);
        var right = new Mock("A", 1);
        Assert.That(left != right, Is.True);
    }

    #endregion
}