namespace Throughline.Simulator.Configuration;

/// <summary>
///     How one client-owner's order management system behaves: when it sends orders, what they
///     look like, and how often it sends bad ones
/// </summary>
public sealed class CustomerProfile
{
    /// <summary>
    ///     Per-customer stop switch
    /// </summary>
    public bool Enabled { get; set; } = true;

    public int OwnerId { get; set; }

    /// <summary>
    ///     Prefix for the reference and PO numbers this customer generates, e.g. <c>HFO</c>
    /// </summary>
    public string ReferencePrefix { get; set; } = "";

    /// <summary>
    ///     IANA time zone id; the hourly and weekday curves are in the customer's local time
    /// </summary>
    public string TimeZone { get; set; } = "";

    /// <summary>
    ///     Orders on an average day; the weekday curve raises or lowers it per day
    /// </summary>
    public double OrdersPerDay { get; set; }

    /// <summary>
    ///     24 relative weights, one per local hour starting at midnight
    /// </summary>
    public double[] HourlyWeights { get; set; } = [];

    /// <summary>
    ///     7 relative weights, Sunday through Saturday
    /// </summary>
    public double[] WeekdayWeights { get; set; } = [];

    public SizeProfile LinesPerOrder { get; set; } = new();
    public SizeProfile QuantityPerLine { get; set; } = new();
    public CatalogProfile Catalog { get; set; } = new();
    public FaultRates Faults { get; set; } = new();
    public RetryProfile Retry { get; set; } = new();
    public BuyingProfile Buying { get; set; } = new();
    public AllocationProfile Allocation { get; set; } = new();

    /// <summary>
    ///     Client id of the customer's OMS app registration
    /// </summary>
    public string OmsClientId { get; set; } = "";

    public string OmsClientSecret { get; set; } = "";
}

/// <summary>
///     A count described by its median and 95th percentile
/// </summary>
public sealed class SizeProfile
{
    public int P50 { get; set; } = 1;
    public int P95 { get; set; } = 1;
}

/// <summary>
///     How the customer's order lines spread across its SKUs, which are read from Throughline
/// </summary>
public sealed class CatalogProfile
{
    /// <summary>
    ///     Share of the catalog, most popular first, that receives <see cref="HotLineShare" /> of
    ///     order lines
    /// </summary>
    public double HotSkuShare { get; set; } = 0.2;

    public double HotLineShare { get; set; } = 0.8;
}

/// <summary>
///     Share of orders, between 0 and 1, sent as each kind of resend or bad request
/// </summary>
public sealed class FaultRates
{
    public double IdenticalResend { get; set; }
    public double ChangedResend { get; set; }
    public double UnknownSku { get; set; }
    public double MissingRequiredField { get; set; }
    public double InvalidQuantity { get; set; }

    public double Total => IdenticalResend + ChangedResend + UnknownSku + MissingRequiredField + InvalidQuantity;
}

/// <summary>
///     How the customer's buyer restocks the warehouse: a weekly purchase order that replaces what sold
///     plus a safety margin, shipped by the supplier after a lead time
/// </summary>
public sealed class BuyingProfile
{
    public bool Enabled { get; set; }

    /// <summary>
    ///     The supplier named on inbound deliveries
    /// </summary>
    public string SupplierName { get; set; } = "";

    /// <summary>
    ///     Weekly review, in the customer's time zone
    /// </summary>
    public DayOfWeek ReviewDay { get; set; } = DayOfWeek.Monday;

    public TimeSpan ReviewTime { get; set; } = TimeSpan.FromHours(9);

    /// <summary>
    ///     Ordered on top of last week's sales, e.g. 0.2 for 20%
    /// </summary>
    public double SafetyMargin { get; set; } = 0.2;

    /// <summary>
    ///     Units per case; orders are rounded up to whole cases
    /// </summary>
    public int CasePack { get; set; } = 12;

    /// <summary>
    ///     Most units of one SKU on a pallet; each pallet holds a single SKU
    /// </summary>
    public int UnitsPerPallet { get; set; } = 48;

    public int PalletsPerTrailer { get; set; } = 26;

    public int LeadTimeDaysMin { get; set; } = 5;
    public int LeadTimeDaysMax { get; set; } = 10;

    /// <summary>
    ///     Share of trailers that arrive one to three days late
    /// </summary>
    public double LateShipmentRate { get; set; } = 0.05;

    /// <summary>
    ///     Weeks of expected demand in the opening order, placed once when nothing has been ordered yet
    /// </summary>
    public int OpeningWeeks { get; set; } = 3;
}

/// <summary>
///     When the facility's allocators release the customer's orders for allocation. Used only when the facility
///     has allocators.
/// </summary>
public sealed class AllocationProfile
{
    public bool Enabled { get; set; }

    /// <summary>
    ///     Latest time of day, in the customer's time zone, an order can be released and still ship that day.
    ///     Waves at or before it are same-day waves; later waves release orders for the next day.
    /// </summary>
    public TimeSpan SameDayCutoff { get; set; } = TimeSpan.FromHours(14);

    public List<WaveTimesOptions> Waves { get; set; } = [];
}

/// <summary>
///     Times of day, in the customer's time zone, a wave is released on each of <see cref="Days" />
/// </summary>
public sealed class WaveTimesOptions
{
    public DayOfWeek[] Days { get; set; } = [];

    public TimeSpan[] Times { get; set; } = [];
}

/// <summary>
///     How a client retries timeouts, 5xx and 429 responses
/// </summary>
public sealed class RetryProfile
{
    public int MaxRetryAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 2;
    public double AttemptTimeoutSeconds { get; set; } = 10;
}
