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

        if (string.IsNullOrWhiteSpace(options.CatalogConnectionString))
            failures.Add("Simulator:CatalogConnectionString is required");

        var enabledCustomers = options.Customers.Where(c => c.Value.Enabled).ToList();

        if (enabledCustomers.Count == 0)
            failures.Add("Simulator:Customers must contain at least one enabled customer");

        foreach (var (customerName, profile) in enabledCustomers)
            ValidateCustomer($"Simulator:Customers:{customerName}", profile, failures);

        var buying = enabledCustomers.Any(c => c.Value.Buying.Enabled);
        var receiving = options.Facility.Receivers.Count > 0;
        var allocating = options.Facility.Allocators.Count > 0;

        if ((buying || receiving) && string.IsNullOrWhiteSpace(options.LedgerConnectionString))
            failures.Add("Simulator:LedgerConnectionString is required when a customer buys stock or the facility has receivers");

        // buyers plan deliveries around the dock's hours, locations and carriers too; allocators only
        // need the shifts
        if (buying || receiving)
            ValidateFacility("Simulator:Facility", options.Facility, failures);
        else if (allocating)
            ValidateShifts("Simulator:Facility", options.Facility, failures);

        if (receiving)
            ValidateReceivers("Simulator:Facility", options.Facility, failures);

        if (allocating)
        {
            ValidateAllocators("Simulator:Facility", options.Facility, failures);

            foreach (var (customerName, profile) in enabledCustomers.Where(c => c.Value.Allocation.Enabled))
                ValidateAllocation($"Simulator:Customers:{customerName}:Allocation", profile.Allocation, failures);
        }

        if ((receiving || allocating) && options.Facility.Retry.MaxRetryAttempts is < 1 or > 10)
            failures.Add("Simulator:Facility:Retry:MaxRetryAttempts must be between 1 and 10");

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

        ValidateCatalog($"{path}:Catalog", profile.Catalog, failures);
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

        if (profile.Buying.Enabled)
            ValidateBuying($"{path}:Buying", profile.Buying, failures);
    }

    private static void ValidateBuying(string path, BuyingProfile buying, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(buying.SupplierName))
            failures.Add($"{path}:SupplierName is required");

        if (buying.ReviewTime < TimeSpan.Zero || buying.ReviewTime >= TimeSpan.FromDays(1))
            failures.Add($"{path}:ReviewTime must be a time of day");

        if (buying.SafetyMargin < 0)
            failures.Add($"{path}:SafetyMargin must not be negative");

        if (buying.CasePack < 1)
            failures.Add($"{path}:CasePack must be at least 1");

        if (buying.UnitsPerPallet < buying.CasePack)
            failures.Add($"{path}:UnitsPerPallet must be at least one case");

        if (buying.PalletsPerTrailer < 1)
            failures.Add($"{path}:PalletsPerTrailer must be at least 1");

        if (buying.LeadTimeDaysMin < 0 || buying.LeadTimeDaysMax < buying.LeadTimeDaysMin)
            failures.Add($"{path}:LeadTimeDaysMin must not be negative or greater than LeadTimeDaysMax");

        if (buying.LateShipmentRate is < 0 or > 1)
            failures.Add($"{path}:LateShipmentRate must be between 0 and 1");

        if (buying.OpeningWeeks < 0)
            failures.Add($"{path}:OpeningWeeks must not be negative");
    }

    private static void ValidateFacility(string path, FacilityOptions facility, List<string> failures)
    {
        ValidateShifts(path, facility, failures);

        if (string.IsNullOrWhiteSpace(facility.PutawayLocationPrefix) || facility.PutawayLocationCount < 1)
            failures.Add($"{path}:PutawayLocationPrefix and a PutawayLocationCount of at least 1 are required");

        if (facility.CarrierScacs.Length == 0 || facility.CarrierScacs.Any(string.IsNullOrWhiteSpace))
            failures.Add($"{path}:CarrierScacs must list at least one carrier");
    }

    private static void ValidateShifts(string path, FacilityOptions facility, List<string> failures)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(facility.TimeZone, out _))
            failures.Add($"{path}:TimeZone '{facility.TimeZone}' is not a known time zone");

        if (facility.DockShifts.Count == 0)
            failures.Add($"{path}:DockShifts must contain at least one shift");

        if (facility.DockShifts.Any(s =>
                s.Days.Length == 0 || s.Start < TimeSpan.Zero || s.End > TimeSpan.FromDays(1) || s.End <= s.Start))
            failures.Add($"{path}:DockShifts each need days, and a start before an end within the same day");
    }

    private static void ValidateReceivers(string path, FacilityOptions facility, List<string> failures)
    {
        if (facility.MinutesPerTrailer <= 0)
            failures.Add($"{path}:MinutesPerTrailer must be greater than 0");

        if (string.IsNullOrWhiteSpace(facility.UserTenantId))
            failures.Add($"{path}:UserTenantId is required when the facility has receivers");

        if (string.IsNullOrWhiteSpace(facility.UserClientId))
            failures.Add($"{path}:UserClientId is required when the facility has receivers");

        if (facility.Receivers.Any(r => string.IsNullOrWhiteSpace(r.Username) || string.IsNullOrWhiteSpace(r.Password)))
            failures.Add($"{path}:Receivers each need a Username and Password");

        if (facility.Receivers.Select(r => r.Username).Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
            facility.Receivers.Count)
            failures.Add($"{path}:Receivers must have different usernames");
    }

    private static void ValidateAllocators(string path, FacilityOptions facility, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(facility.UserTenantId))
            failures.Add($"{path}:UserTenantId is required when the facility has allocators");

        if (string.IsNullOrWhiteSpace(facility.UserClientId))
            failures.Add($"{path}:UserClientId is required when the facility has allocators");

        if (facility.Allocators.Any(a => string.IsNullOrWhiteSpace(a.Username) || string.IsNullOrWhiteSpace(a.Password)))
            failures.Add($"{path}:Allocators each need a Username and Password");

        if (facility.Allocators.Any(a =>
                a.DockShifts.Length == 0 || a.DockShifts.Any(i => i < 0 || i >= facility.DockShifts.Count)))
            failures.Add($"{path}:Allocators each need DockShifts, as indexes of the facility's DockShifts");

        var shifts = facility.Allocators.SelectMany(a => a.DockShifts.Distinct()).ToList();

        if (shifts.Distinct().Count() != shifts.Count)
            failures.Add($"{path}:Allocators must not share a dock shift");

        // each staff member has one token and one HTTP client, keyed by username
        var usernames = facility.Receivers.Select(r => r.Username)
            .Concat(facility.Allocators.Select(a => a.Username))
            .ToList();

        if (usernames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != usernames.Count)
            failures.Add($"{path}:Allocators must have different usernames from each other and from Receivers");
    }

    private static void ValidateAllocation(string path, AllocationProfile allocation, List<string> failures)
    {
        if (allocation.SameDayCutoff < TimeSpan.Zero || allocation.SameDayCutoff >= TimeSpan.FromDays(1))
            failures.Add($"{path}:SameDayCutoff must be a time of day");

        if (allocation.Waves.Count == 0)
            failures.Add($"{path}:Waves must contain at least one wave");

        if (allocation.Waves.Any(w =>
                w.Days.Length == 0 || w.Times.Length == 0 ||
                w.Times.Any(t => t < TimeSpan.Zero || t >= TimeSpan.FromDays(1))))
            failures.Add($"{path}:Waves each need days, and times of day");
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

    private static void ValidateCatalog(string path, CatalogProfile catalog, List<string> failures)
    {
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
