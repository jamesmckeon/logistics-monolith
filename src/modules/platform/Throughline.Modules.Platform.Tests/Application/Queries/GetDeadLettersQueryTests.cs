using System.Text.Json;
using Moq;
using Throughline.Common.Results;
using Throughline.Modules.Platform.Application.Common;
using Throughline.Modules.Platform.Application.Models;
using Throughline.Modules.Platform.Application.Queries;
using Wolverine;
using Wolverine.Runtime.Serialization;

namespace Throughline.Modules.Platform.Tests.Application.Queries;

[Category("Unit")]
internal sealed class GetDeadLettersQueryTests
{
    private const string MessageType = "Throughline.Modules.Ordering.Contracts.Events.OrderConfirmedIntegrationEvent";

    private static readonly CancellationTokenSource TokenSource = new();
    private static readonly DateTimeOffset SentAt = new(2025, 11, 21, 15, 40, 3, TimeSpan.Zero);

    private Mock<IDeadLetterStore> _store;
    private GetDeadLettersQuery _sut;
    private static CancellationToken Token => TokenSource.Token;

    [SetUp]
    public void SetUp()
    {
        _store = new Mock<IDeadLetterStore>(MockBehavior.Strict);

        _sut = new GetDeadLettersQuery(_store.Object);
    }

    #region GetDeadLettersAsync

    [Test]
    public async Task GetDeadLettersAsync_NoDeadLetters_ReturnsEmptyList()
    {
        GivenDeadLetters();

        var actual = await _sut.GetDeadLettersAsync(null, null, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value?.DeadLetters, Is.Empty);
        });
    }

    [Test]
    public async Task GetDeadLettersAsync_SentAtDifferent_ReturnsNewestSentFirst()
    {
        var oldest = CreateDeadLetter(new { ownerId = 1 }, SentAt.AddMinutes(-10));
        var newest = CreateDeadLetter(new { ownerId = 1 }, SentAt);
        var middle = CreateDeadLetter(new { ownerId = 1 }, SentAt.AddMinutes(-5));
        GivenDeadLetters(oldest, newest, middle);

        var actual = await _sut.GetDeadLettersAsync(null, null, Token);

        Assert.That(actual.Value?.DeadLetters.Select(d => d.DeadLetterId),
            Is.EqualTo([newest.Id, middle.Id, oldest.Id]));
    }

    [Test]
    public async Task GetDeadLettersAsync_NoSentAt_ReturnsItLast()
    {
        var notSent = CreateDeadLetter(new { ownerId = 1 }, null);
        var sent = CreateDeadLetter(new { ownerId = 1 }, SentAt);
        GivenDeadLetters(notSent, sent);

        var actual = await _sut.GetDeadLettersAsync(null, null, Token);

        Assert.That(actual.Value?.DeadLetters.Select(d => d.DeadLetterId), Is.EqualTo([sent.Id, notSent.Id]));
    }

    [Test]
    public async Task GetDeadLettersAsync_DeadLetterFound_MapsFields()
    {
        var deadLetter = CreateDeadLetter(new { ownerId = 2, orderId = "0193a7c2" }, SentAt, true);
        GivenDeadLetters(deadLetter);

        var actual = await _sut.GetDeadLettersAsync(null, null, Token);

        var model = actual.Value?.DeadLetters.Single();

        Assert.Multiple(() =>
        {
            Assert.That(model?.DeadLetterId, Is.EqualTo(deadLetter.Id));
            Assert.That(model?.SentAt, Is.EqualTo(SentAt));
            Assert.That(model?.Module, Is.EqualTo(GetDeadLettersQuery.DefaultModule));
            Assert.That(model?.MessageType, Is.EqualTo(MessageType));
            Assert.That(model?.ExceptionType, Is.EqualTo(deadLetter.ExceptionType));
            Assert.That(model?.ExceptionMessage, Is.EqualTo(deadLetter.ExceptionMessage));
            Assert.That(model?.ReplayQueued, Is.True);
            Assert.That(model?.Message?.GetProperty("ownerId").GetInt32(), Is.EqualTo(2));
            Assert.That(model?.Message?.GetProperty("orderId").GetString(), Is.EqualTo("0193a7c2"));
        });
    }

    [Test]
    public async Task GetDeadLettersAsync_ReplayableNull_ReplayNotQueued()
    {
        GivenDeadLetters(CreateDeadLetter(new { ownerId = 1 }, SentAt, null));

        var actual = await _sut.GetDeadLettersAsync(null, null, Token);

        Assert.That(actual.Value?.DeadLetters.Single().ReplayQueued, Is.False);
    }

    [Test]
    public async Task GetDeadLettersAsync_OwnerIdGiven_ReturnsOnlyThatOwnersDeadLetters()
    {
        var ownerOne = CreateDeadLetter(new { ownerId = 1 }, SentAt);
        var ownerTwo = CreateDeadLetter(new { ownerId = 2 }, SentAt);
        GivenDeadLetters(ownerOne, ownerTwo);

        var actual = await _sut.GetDeadLettersAsync(2, null, Token);

        Assert.That(actual.Value?.DeadLetters.Select(d => d.DeadLetterId), Is.EqualTo([ownerTwo.Id]));
    }

    [Test]
    public async Task GetDeadLettersAsync_OwnerPropertyPascalCase_MatchesIgnoringCase()
    {
        var deadLetter = CreateDeadLetter(new { OwnerId = 2 }, SentAt);
        GivenDeadLetters(deadLetter);

        var actual = await _sut.GetDeadLettersAsync(2, null, Token);

        Assert.That(actual.Value?.DeadLetters.Select(d => d.DeadLetterId), Is.EqualTo([deadLetter.Id]));
    }

    [Test]
    public async Task GetDeadLettersAsync_OwnerIdGivenAndMessageHasNoOwner_ExcludesIt()
    {
        var owned = CreateDeadLetter(new { ownerId = 2 }, SentAt);
        var unowned = CreateDeadLetter(new { receiptId = "R-1" }, SentAt);
        GivenDeadLetters(owned, unowned);

        var actual = await _sut.GetDeadLettersAsync(2, null, Token);

        Assert.That(actual.Value?.DeadLetters.Select(d => d.DeadLetterId), Is.EqualTo([owned.Id]));
    }

    [Test]
    public async Task GetDeadLettersAsync_NoOwnerId_IncludesMessagesWithoutOwner()
    {
        var owned = CreateDeadLetter(new { ownerId = 2 }, SentAt);
        var unowned = CreateDeadLetter(new { receiptId = "R-1" }, SentAt);
        GivenDeadLetters(owned, unowned);

        var actual = await _sut.GetDeadLettersAsync(null, null, Token);

        Assert.That(actual.Value?.DeadLetters.Select(d => d.DeadLetterId),
            Is.EquivalentTo([owned.Id, unowned.Id]));
    }

    [TestCase(GetDeadLettersQuery.DefaultModule)]
    [TestCase("inventory")]
    [TestCase("INVENTORY")]
    public async Task GetDeadLettersAsync_ModuleMatchesIgnoringCase_ReturnsDeadLetters(string module)
    {
        var deadLetter = CreateDeadLetter(new { ownerId = 1 }, SentAt);
        GivenDeadLetters(deadLetter);

        var actual = await _sut.GetDeadLettersAsync(null, module, Token);

        Assert.That(actual.Value?.DeadLetters.Select(d => d.DeadLetterId), Is.EqualTo([deadLetter.Id]));
    }

    [Test]
    public async Task GetDeadLettersAsync_ModuleNotMatching_ReturnsEmptyList()
    {
        GivenDeadLetters(CreateDeadLetter(new { ownerId = 1 }, SentAt));

        var actual = await _sut.GetDeadLettersAsync(null, "Receiving", Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value?.DeadLetters, Is.Empty);
        });
    }

    #endregion

    [Test]
    public async Task GetDeadLetterAsync_DeadLetterExists_ReturnsIt()
    {
        var deadLetter = CreateDeadLetter(new { ownerId = 2 }, SentAt);
        _store.Setup(s => s.GetDeadLetterAsync(deadLetter.Id, Token)).ReturnsAsync(deadLetter);

        var actual = await _sut.GetDeadLetterAsync(deadLetter.Id, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.True);
            Assert.That(actual.Value?.DeadLetterId, Is.EqualTo(deadLetter.Id));
            Assert.That(actual.Value?.Module, Is.EqualTo(GetDeadLettersQuery.DefaultModule));
            Assert.That(actual.Value?.Message?.GetProperty("ownerId").GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetDeadLetterAsync_NoDeadLetterWithId_ReturnsNotFound()
    {
        var deadLetterId = Guid.CreateVersion7();
        _store.Setup(s => s.GetDeadLetterAsync(deadLetterId, Token)).ReturnsAsync((DeadLetterRecord?)null);

        var actual = await _sut.GetDeadLetterAsync(deadLetterId, Token);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Succeeded, Is.False);
            Assert.That(actual.ErrorType, Is.EqualTo(ErrorType.NotFound));
        });
    }

    private void GivenDeadLetters(params DeadLetterRecord[] deadLetters) =>
        _store.Setup(s => s.GetDeadLettersAsync(Token)).ReturnsAsync(deadLetters);

    /// <summary>
    ///     A row as Wolverine stores it: the message's JSON inside an envelope, serialized into the body
    /// </summary>
    private static DeadLetterRecord CreateDeadLetter(object message, DateTimeOffset? sentAt,
        bool? replayable = false)
    {
        var envelope = new Envelope
        {
            Id = Guid.CreateVersion7(),
            MessageType = MessageType,
            ContentType = "application/json",
            Data = JsonSerializer.SerializeToUtf8Bytes(message)
        };

        return new DeadLetterRecord
        {
            Id = envelope.Id,
            Body = EnvelopeSerializer.Serialize(envelope),
            MessageType = MessageType,
            ExceptionType = "Throughline.Common.Events.MissingDependencyException",
            ExceptionMessage = "The following skus were not found: HFO-UNKNOWN-17",
            SentAt = sentAt,
            Replayable = replayable
        };
    }
}
