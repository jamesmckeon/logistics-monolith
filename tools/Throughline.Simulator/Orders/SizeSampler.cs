using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Orders;

/// <summary>
///     Draws whole-number sizes (lines per order, quantity per line) from a log-normal distribution
///     whose median is P50 and whose 95th percentile is P95: mostly small, with a long tail
/// </summary>
internal static class SizeSampler
{
    // the standard normal's 95th percentile
    private const double Z95 = 1.6448536269514722;

    public static int Sample(SizeProfile profile, Random random, int max)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(random);

        if (profile.P95 <= profile.P50)
            return Math.Clamp(profile.P50, 1, max);

        var mu = Math.Log(profile.P50);
        var sigma = (Math.Log(profile.P95) - mu) / Z95;
        var value = Math.Round(Math.Exp(mu + sigma * StandardNormal(random)));

        return (int)Math.Clamp(value, 1, max);
    }

    // Box-Muller transform
    private static double StandardNormal(Random random)
    {
        var u1 = 1.0 - random.NextDouble();
        var u2 = random.NextDouble();

        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
