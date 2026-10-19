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
///     How the customer's OMS retries timeouts, 5xx and 429 responses
/// </summary>
public sealed class RetryProfile
{
    public int MaxRetryAttempts { get; set; } = 3;
    public double BaseDelaySeconds { get; set; } = 2;
    public double AttemptTimeoutSeconds { get; set; } = 10;
}
