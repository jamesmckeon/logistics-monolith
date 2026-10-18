using Microsoft.Extensions.Options;

namespace Throughline.Simulator.Configuration;

/// <summary>
///     Checks the configuration at startup so a bad profile stops the simulator with every problem
///     listed, rather than failing hours later. Nothing is checked while the simulator is disabled.
/// </summary>
internal sealed class SimulatorOptionsValidator : IValidateOptions<SimulatorOptions>
{
    public const int MaxLinesPerOrder = 50;

    public ValidateOptionsResult Validate(string? name, SimulatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (options.ThroughlineBaseUrl is not { IsAbsoluteUri: true })
            failures.Add("Simulator:ThroughlineBaseUrl must be an absolute URL");

        if (string.IsNullOrWhiteSpace(options.TenantId))
            failures.Add("Simulator:TenantId is required");

        if (string.IsNullOrWhiteSpace(options.ApiScope))
            failures.Add("Simulator:ApiScope is required");

        var enabledCustomers = options.Customers.Where(c => c.Value.Enabled).ToList();

        if (enabledCustomers.Count == 0)
            failures.Add("Simulator:Customers must contain at least one enabled customer");

        foreach (var (customerName, profile) in enabledCustomers)
            ValidateCustomer($"Simulator:Customers:{customerName}", profile, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateCustomer(string path, CustomerProfile profile, List<string> failures)
    {
        if (profile.OwnerId <= 0)
            failures.Add($"{path}:OwnerId must be greater than 0");

        if (string.IsNullOrWhiteSpace(profile.ReferencePrefix))
            failures.Add($"{path}:ReferencePrefix is required");

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(profile.TimeZone, out _))
            failures.Add($"{path}:TimeZone '{profile.TimeZone}' is not a known time zone");

        if (profile.OrdersPerDay <= 0)
            failures.Add($"{path}:OrdersPerDay must be greater than 0");

        ValidateWeights($"{path}:HourlyWeights", profile.HourlyWeights, 24, failures);
        ValidateWeights($"{path}:WeekdayWeights", profile.WeekdayWeights, 7, failures);
        ValidateSize($"{path}:LinesPerOrder", profile.LinesPerOrder, failures);
        ValidateSize($"{path}:QuantityPerLine", profile.QuantityPerLine, failures);

        if (profile.LinesPerOrder.P95 > MaxLinesPerOrder)
            failures.Add($"{path}:LinesPerOrder:P95 must be at most {MaxLinesPerOrder}");

        ValidateCatalog($"{path}:Catalog", profile.Catalog, profile.LinesPerOrder, failures);
        ValidateFaults($"{path}:Faults", profile.Faults, failures);

        if (profile.Retry.MaxRetryAttempts is < 1 or > 10)
            failures.Add($"{path}:Retry:MaxRetryAttempts must be between 1 and 10");

        if (profile.Retry.BaseDelaySeconds <= 0)
            failures.Add($"{path}:Retry:BaseDelaySeconds must be greater than 0");

        if (profile.Retry.AttemptTimeoutSeconds <= 0)
            failures.Add($"{path}:Retry:AttemptTimeoutSeconds must be greater than 0");

        if (string.IsNullOrWhiteSpace(profile.OmsClientId))
            failures.Add($"{path}:OmsClientId is required");

        if (string.IsNullOrWhiteSpace(profile.OmsClientSecret))
            failures.Add($"{path}:OmsClientSecret is required");
    }

    private static void ValidateWeights(string path, double[] weights, int expectedCount, List<string> failures)
    {
        if (weights.Length != expectedCount)
            failures.Add($"{path} must have {expectedCount} values, not {weights.Length}");
        else if (weights.Any(w => w < 0) || weights.Sum() <= 0)
            failures.Add($"{path} must not be negative, and at least one must be greater than 0");
    }

    private static void ValidateSize(string path, SizeProfile size, List<string> failures)
    {
        if (size.P50 < 1)
            failures.Add($"{path}:P50 must be at least 1");

        if (size.P95 < size.P50)
            failures.Add($"{path}:P95 must not be less than P50");
    }

    private static void ValidateCatalog(string path, CatalogProfile catalog, SizeProfile lines, List<string> failures)
    {
        if (catalog.GeneratedSkuCount < 0)
            failures.Add($"{path}:GeneratedSkuCount must not be negative");

        if (catalog.GeneratedSkuCount > 0 && string.IsNullOrWhiteSpace(catalog.GeneratedSkuPrefix))
            failures.Add($"{path}:GeneratedSkuPrefix is required when GeneratedSkuCount is greater than 0");

        var size = catalog.NamedSkus.Length + Math.Max(catalog.GeneratedSkuCount, 0);

        // an order can't repeat a SKU, so the catalog must hold at least the largest order
        if (size < Math.Min(lines.P95 * 2, MaxLinesPerOrder))
            failures.Add($"{path} must hold at least {Math.Min(lines.P95 * 2, MaxLinesPerOrder)} SKUs");

        if (catalog.HotSkuShare is <= 0 or > 1)
            failures.Add($"{path}:HotSkuShare must be greater than 0 and at most 1");

        if (catalog.HotLineShare is < 0 or > 1)
            failures.Add($"{path}:HotLineShare must be between 0 and 1");
    }

    private static void ValidateFaults(string path, FaultRates faults, List<string> failures)
    {
        double[] rates =
        [
            faults.IdenticalResend, faults.ChangedResend, faults.UnknownSku,
            faults.MissingRequiredField, faults.InvalidQuantity
        ];

        if (rates.Any(r => r is < 0 or > 1))
            failures.Add($"{path} rates must be between 0 and 1");

        if (faults.Total > 1)
            failures.Add($"{path} rates must not add up to more than 1");
    }
}
