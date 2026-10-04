using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Tests.Domain.Common;

[Category("Unit")]
public sealed class UniqueIdTests
{
    #region Constructor

    [Test]
    public void Constructor_Version7Guid_SetsValue()
    {
        var guid = Guid.CreateVersion7();

        var uniqueId = new UniqueId(guid);

        Assert.That(uniqueId.Value, Is.EqualTo(guid));
    }

    [TestCase("00000000-0000-0000-0000-000000000000")]
    [TestCase("6ba7b810-9dad-11d1-80b4-00c04fd430c8")]
    [TestCase("3f2504e0-4f89-41d3-9a0c-0305e82c3301")]
    public void Constructor_NonVersion7Guid_ThrowsArgumentException(string guid)
    {
        var value = Guid.Parse(guid);

        Assert.That(() => new UniqueId(value), Throws.ArgumentException);
    }

    #endregion
}
