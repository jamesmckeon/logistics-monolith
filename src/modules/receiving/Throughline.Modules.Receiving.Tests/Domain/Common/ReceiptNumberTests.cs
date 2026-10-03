using Throughline.Common.Models;
using Throughline.Modules.Receiving.Domain.Common;

namespace Throughline.Modules.Receiving.Tests.Domain.Common;

[Category("Unit")]
public sealed class ReceiptNumberTests
{
    #region Create

    [Test]
    public void Create_NoPreviousReceiptNumber_ReturnsFirstInSequence()
    {
        var receiptNumber = ReceiptNumber.Create(null);

        Assert.That(receiptNumber.Value, Is.EqualTo("RCPT-0000000001"));
    }

    [Test]
    public void Create_PreviousReceiptNumber_ReturnsNextInSequence()
    {
        var previous = ReceiptNumber.Create(null);

        var receiptNumber = ReceiptNumber.Create(previous);

        Assert.That(receiptNumber.Value, Is.EqualTo("RCPT-0000000002"));
    }

    [Test]
    public void Create_NoPreviousReceiptNumber_SetsCreatedOnToCurrentTime()
    {
        var before = AppDateTime.Now;

        var receiptNumber = ReceiptNumber.Create(null);

        var after = AppDateTime.Now;
        Assert.That(receiptNumber.CreatedOn.Value, Is.InRange(before.Value, after.Value));
    }

    #endregion
}
