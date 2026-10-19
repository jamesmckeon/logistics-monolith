namespace Throughline.Simulator.Configuration;

/// <summary>
///     Root of the "Simulator" configuration section. Environment-specific values (the API's URL,
///     the tenant, client ids and secrets) come from environment variables or user secrets, never
///     from committed configuration.
/// </summary>
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>
    ///     The stop switch: when false, the simulator sends nothing
    /// </summary>
    public bool Enabled { get; set; } = true;

    public Uri? ThroughlineBaseUrl { get; set; }

    /// <summary>
    ///     Entra tenant that issues tokens to the customers' OMS applications
    /// </summary>
    public string TenantId { get; set; } = "";

    /// <summary>
    ///     Scope requested for Throughline, e.g. <c>api://{api-client-id}/.default</c>
    /// </summary>
    public string ApiScope { get; set; } = "";

    /// <summary>
    ///     Throughline's database, signed in as a role that can only read <c>inventory.skus</c>
    /// </summary>
    public string CatalogConnectionString { get; set; } = "";

    /// <summary>
    ///     The simulator's own database (sales history, purchase orders, inbound shipments), never
    ///     Throughline's. Required when any customer buys stock or the facility has receivers.
    /// </summary>
    public string LedgerConnectionString { get; set; } = "";

    public FacilityOptions Facility { get; set; } = new();

    /// <summary>
    ///     Customer profiles keyed by a short name, e.g. <c>HarrowFinch</c>
    /// </summary>
    public Dictionary<string, CustomerProfile> Customers { get; set; } = new();
}
