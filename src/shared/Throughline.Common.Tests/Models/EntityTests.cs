using Throughline.Common.Models;

namespace Throughline.Common.Tests.Models;

[Category("Unit")]
public sealed class EntityTests
{
    private static readonly Guid IdA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid IdB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    #region Equals

    [Test]
    public void Equals_SameId_ReturnsTrue()
    {
        var left = new MockEntity(IdA);
        var right = new MockEntity(IdA);
        Assert.That(left.Equals(right), Is.True);
    }

    [Test]
    public void Equals_SameIdDifferentMutableState_ReturnsTrue()
    {
        var left = new MockEntity(IdA) { State = "confirmed" };
        var right = new MockEntity(IdA) { State = "allocated" };
        Assert.That(left.Equals(right), Is.True);
    }

    [Test]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var left = new MockEntity(IdA);
        var right = new MockEntity(IdB);
        Assert.That(left.Equals(right), Is.False);
    }

    [Test]
    public void Equals_Null_ReturnsFalse()
    {
        var left = new MockEntity(IdA);
        Assert.That(left.Equals(null), Is.False);
    }

    #endregion

    #region Equality Operators

    [Test]
    public void EqualityOperator_SameId_ReturnsTrue()
    {
        var left = new MockEntity(IdA);
        var right = new MockEntity(IdA);
        Assert.That(left == right, Is.True);
    }

    [Test]
    public void EqualityOperator_DifferentId_ReturnsFalse()
    {
        var left = new MockEntity(IdA);
        var right = new MockEntity(IdB);
        Assert.That(left == right, Is.False);
    }

    [Test]
    public void InequalityOperator_SameId_ReturnsFalse()
    {
        var left = new MockEntity(IdA);
        var right = new MockEntity(IdA);
        Assert.That(left != right, Is.False);
    }

    [Test]
    public void InequalityOperator_DifferentId_ReturnsTrue()
    {
        var left = new MockEntity(IdA);
        var right = new MockEntity(IdB);
        Assert.That(left != right, Is.True);
    }

    #endregion

    #region GetHashCode

    [Test]
    public void GetHashCode_SameId_ReturnsSameHashCode()
    {
        var left = new MockEntity(IdA);
        var right = new MockEntity(IdA);
        Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
    }

    [Test]
    public void GetHashCode_SameIdDifferentMutableState_ReturnsSameHashCode()
    {
        var left = new MockEntity(IdA) { State = "confirmed" };
        var right = new MockEntity(IdA) { State = "allocated" };
        Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
    }

    #endregion

    #region Construction

    [Test]
    public void Constructor_NullId_Throws()
    {
        Assert.That(() => new MockRefEntity(null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Id_ExposesConstructedIdentity()
    {
        var sut = new MockEntity(IdA);
        Assert.That(sut.Id, Is.EqualTo(IdA));
    }

    #endregion

    private sealed class MockEntity(Guid id) : Entity<Guid>(id)
    {
        public string? State { get; init; }
    }

    private sealed class MockRefEntity(string id) : Entity<string>(id);
}
