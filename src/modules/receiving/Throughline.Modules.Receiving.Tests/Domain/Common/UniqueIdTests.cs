using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Tests.Domain.Common;

[Category("Unit")]
public sealed class UniqueIdTests
{
    #region Constructor

    [Test]
    public void Constructor_NonEmptyGuid_SetsValue()
    {
        var guid = Guid.NewGuid();

        var uniqueId = new UniqueId(guid);

        Assert.That(uniqueId.Value, Is.EqualTo(guid));
    }

    [Test]
    public void Constructor_EmptyGuid_ThrowsArgumentException()
    {
        Assert.That(() => new UniqueId(Guid.Empty), Throws.ArgumentException);
    }

    #endregion
}