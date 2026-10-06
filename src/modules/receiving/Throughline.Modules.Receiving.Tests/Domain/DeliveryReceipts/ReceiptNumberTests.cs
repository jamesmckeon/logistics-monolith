using Throughline.Modules.Receiving.Domain.DeliveryReceipts;

namespace Throughline.Modules.Receiving.Tests.Domain.DeliveryReceipts;

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

    #endregion

    #region Constructor

    [Test]
    public void Constructor_ValidFormat_SetsValue()
    {
        var receiptNumber = new ReceiptNumber("RCPT-0000000042");

        Assert.That(receiptNumber.Value, Is.EqualTo("RCPT-0000000042"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("RCPT-42")]
    [TestCase("RCPT-00000000042")]
    [TestCase("XXXX-0000000042")]
    [TestCase("RCPT0000000042")]
    [TestCase("RCPT-00000000A2")]
    [TestCase("rcpt-0000000042")]
    [TestCase(" RCPT-0000000042")]
    [TestCase("RCPT-0000000042 ")]
    public void Constructor_InvalidFormat_ThrowsArgumentException(string value)
    {
        Assert.That(() => new ReceiptNumber(value), Throws.ArgumentException);
    }

    #endregion

    #region FromSequence

    [TestCase(1, "RCPT-0000000001")]
    [TestCase(42, "RCPT-0000000042")]
    [TestCase(9_999_999_999, "RCPT-9999999999")]
    public void FromSequence_TenDigitsOrFewer_ReturnsZeroPaddedReceiptNumber(long sequence, string expected)
    {
        var receiptNumber = ReceiptNumber.FromSequence(sequence);

        Assert.That(receiptNumber.Value, Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void FromSequence_LessThanOne_ThrowsArgumentOutOfRangeException(long sequence)
    {
        Assert.That(() => ReceiptNumber.FromSequence(sequence), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FromSequence_MoreThanTenDigits_ThrowsArgumentException()
    {
        Assert.That(() => ReceiptNumber.FromSequence(10_000_000_000), Throws.ArgumentException);
    }

    #endregion
}