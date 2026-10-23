using Throughline.Simulator.Allocation;

namespace Throughline.Simulator.Tests.Allocation;

[Category("Unit")]
public sealed class WaveSummaryTests
{
    #region From

    [Test]
    public void From_MixedOutcomes_CountsEachOutcome()
    {
        var result = new AllocationResult(
        [
            Allocated(WaveSummary.FullyAllocated),
            Allocated(WaveSummary.FullyAllocated),
            Allocated(WaveSummary.PartiallyAllocated, new ShortageResult("HFO-000001", 3)),
            Allocated(WaveSummary.NotAllocated, new ShortageResult("HFO-000002", 2))
        ]);

        var actual = WaveSummary.From(result);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Count(WaveSummary.FullyAllocated), Is.EqualTo(2));
            Assert.That(actual.Count(WaveSummary.PartiallyAllocated), Is.EqualTo(1));
            Assert.That(actual.Count(WaveSummary.NotAllocated), Is.EqualTo(1));
            Assert.That(actual.Count(WaveSummary.Failed), Is.Zero);
        });
    }

    [Test]
    public void From_Shortages_SumsUnitsStillToAllocate()
    {
        var result = new AllocationResult(
        [
            Allocated(WaveSummary.PartiallyAllocated,
                new ShortageResult("HFO-000001", 3), new ShortageResult("HFO-000002", 1)),
            Allocated(WaveSummary.NotAllocated, new ShortageResult("HFO-000003", 4))
        ]);

        var actual = WaveSummary.From(result);

        Assert.That(actual.UnitsShort, Is.EqualTo(8));
    }

    [Test]
    public void From_FailedOrders_CountsErrorCodes()
    {
        var result = new AllocationResult(
        [
            Failed("ORDER_ALLOCATING"),
            Failed("ORDER_ALLOCATING"),
            Failed("ORDER_NOT_FOUND")
        ]);

        var actual = WaveSummary.From(result);

        Assert.That(actual.ErrorCodes, Is.EquivalentTo(new Dictionary<string, int>
        {
            ["ORDER_ALLOCATING"] = 2,
            ["ORDER_NOT_FOUND"] = 1
        }));
    }

    [Test]
    public void From_NoBody_NothingCounted()
    {
        var actual = WaveSummary.From(null);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Outcomes, Is.Empty);
            Assert.That(actual.UnitsShort, Is.Zero);
            Assert.That(actual.ErrorCodes, Is.Empty);
        });
    }

    #endregion

    private static AllocatedOrderResult Allocated(string status, params ShortageResult[] shortages) =>
        new(Guid.CreateVersion7(), status, shortages, []);

    private static AllocatedOrderResult Failed(string code) =>
        new(Guid.CreateVersion7(), WaveSummary.Failed, [], [new AllocationErrorResult(code, "description")]);
}
