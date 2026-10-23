using System.Net;
using Moq;
using Throughline.Simulator.Allocation;

namespace Throughline.Simulator.Tests.Allocation;

[Category("Unit")]
public sealed class WaveReleaseTests
{
    private const int OwnerId = 1;

    // 120 orders: requests of 50, 50 and 20
    private static readonly IReadOnlyList<Guid> Orders = Enumerable.Range(0, 120).Select(_ => Guid.NewGuid()).ToList();
    private static readonly IReadOnlyList<Guid> FirstRequest = Orders.Take(50).ToList();
    private static readonly IReadOnlyList<Guid> SecondRequest = Orders.Skip(50).Take(50).ToList();
    private static readonly IReadOnlyList<Guid> ThirdRequest = Orders.Skip(100).ToList();

    private Mock<IInventoryClient> _client = null!;
    private WaveRelease _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _client = new Mock<IInventoryClient>(MockBehavior.Strict);
        _sut = new WaveRelease(_client.Object);
    }

    #region SplitIntoRequests

    [Test]
    public void SplitIntoRequests_MoreThanFifty_FiftyAtATimeInOrder()
    {
        var actual = WaveRelease.SplitIntoRequests(Orders, WaveRelease.MaxOrdersPerRequest);

        Assert.That(actual, Is.EqualTo(new[] { FirstRequest, SecondRequest, ThirdRequest }));
    }

    [Test]
    public void SplitIntoRequests_ExactlyFifty_OneRequest()
    {
        var actual = WaveRelease.SplitIntoRequests(FirstRequest, WaveRelease.MaxOrdersPerRequest);

        Assert.That(actual, Is.EqualTo(new[] { FirstRequest }));
    }

    [Test]
    public void SplitIntoRequests_NoOrders_NoRequests()
    {
        var actual = WaveRelease.SplitIntoRequests([], WaveRelease.MaxOrdersPerRequest);

        Assert.That(actual, Is.Empty);
    }

    #endregion

    #region Classify

    [Test]
    public void Classify_Ok_Allocated()
    {
        var actual = WaveRelease.Classify(HttpStatusCode.OK);

        Assert.That(actual, Is.EqualTo(WaveResult.Allocated));
    }

    [Test]
    public void Classify_BadRequest_Rejected()
    {
        var actual = WaveRelease.Classify(HttpStatusCode.BadRequest);

        Assert.That(actual, Is.EqualTo(WaveResult.Rejected));
    }

    [TestCase(HttpStatusCode.Unauthorized, TestName = "Classify_Unauthorized_LeftForNextWave")]
    [TestCase(HttpStatusCode.Forbidden, TestName = "Classify_Forbidden_LeftForNextWave")]
    [TestCase(HttpStatusCode.InternalServerError, TestName = "Classify_InternalServerError_LeftForNextWave")]
    [TestCase(HttpStatusCode.ServiceUnavailable, TestName = "Classify_ServiceUnavailable_LeftForNextWave")]
    public void Classify_SignInRoleOrServerProblem_LeftForNextWave(HttpStatusCode status)
    {
        var actual = WaveRelease.Classify(status);

        Assert.That(actual, Is.EqualTo(WaveResult.LeftForNextWave));
    }

    #endregion

    #region ReleaseAsync

    [Test]
    public async Task ReleaseAsync_EveryRequestAllocated_SendsAllInOrderAndFinishes()
    {
        GivenResponse(FirstRequest, HttpStatusCode.OK);
        GivenResponse(SecondRequest, HttpStatusCode.OK);
        GivenResponse(ThirdRequest, HttpStatusCode.OK);

        var actual = await _sut.ReleaseAsync(OwnerId, Orders, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Requests.Select(r => r.OrderIds),
                Is.EqualTo(new[] { FirstRequest, SecondRequest, ThirdRequest }));
            Assert.That(actual.RequestsPlanned, Is.EqualTo(3));
            Assert.That(actual.Finished, Is.True);
            Assert.That(actual.OrdersReleased, Is.EqualTo(120));
        });
    }

    [Test]
    public async Task ReleaseAsync_ServerErrorOnSecondRequest_StopsBeforeThird()
    {
        GivenResponse(FirstRequest, HttpStatusCode.OK);
        GivenResponse(SecondRequest, HttpStatusCode.InternalServerError);

        var actual = await _sut.ReleaseAsync(OwnerId, Orders, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Requests.Select(r => r.OrderIds), Is.EqualTo(new[] { FirstRequest, SecondRequest }));
            Assert.That(actual.Finished, Is.False);
            Assert.That(actual.OrdersReleased, Is.EqualTo(50));
        });
        _client.Verify(c => c.AllocateAsync(OwnerId, Exactly(ThirdRequest), CancellationToken.None), Times.Never);
    }

    [Test]
    public async Task ReleaseAsync_FirstRequestRejected_StopsAfterIt()
    {
        GivenResponse(FirstRequest, HttpStatusCode.BadRequest);

        var actual = await _sut.ReleaseAsync(OwnerId, Orders, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Requests.Select(r => r.OrderIds), Is.EqualTo(new[] { FirstRequest }));
            Assert.That(actual.Finished, Is.False);
            Assert.That(actual.OrdersReleased, Is.Zero);
        });
    }

    [Test]
    public async Task ReleaseAsync_NoResponse_StopsAndKeepsTheFailure()
    {
        var failure = new HttpRequestException("connection refused");
        _client.Setup(c => c.AllocateAsync(OwnerId, Exactly(FirstRequest), CancellationToken.None))
            .ThrowsAsync(failure);

        var actual = await _sut.ReleaseAsync(OwnerId, Orders, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Requests, Has.Count.EqualTo(1));
            Assert.That(actual.Requests[0].Response, Is.Null);
            Assert.That(actual.Requests[0].Failure, Is.SameAs(failure));
            Assert.That(actual.Finished, Is.False);
        });
    }

    #endregion

    private void GivenResponse(IReadOnlyList<Guid> orderIds, HttpStatusCode status)
    {
        var result = status == HttpStatusCode.OK ? new AllocationResult([]) : null;

        _client.Setup(c => c.AllocateAsync(OwnerId, Exactly(orderIds), CancellationToken.None))
            .ReturnsAsync(new AllocationResponse(status, result, TimeSpan.FromMilliseconds(500)));
    }

    // the release sends its own copies of the order ids, so match the contents, in order
    private static IReadOnlyList<Guid> Exactly(IReadOnlyList<Guid> orderIds) =>
        It.Is<IReadOnlyList<Guid>>(ids => ids.SequenceEqual(orderIds));
}
