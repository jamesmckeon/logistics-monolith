namespace Throughline.Simulator.Configuration;

/// <summary>
///     The distribution center the simulated warehouse staff work in. Shifts belong to the facility,
///     not to customers, and its staff work for whichever customers' work is waiting.
/// </summary>
public sealed class FacilityOptions
{
    /// <summary>
    ///     IANA time zone id; shift times are in the facility's local time
    /// </summary>
    public string TimeZone { get; set; } = "";

    /// <summary>
    ///     When the receiving dock is staffed
    /// </summary>
    public List<ShiftOptions> DockShifts { get; set; } = [];

    /// <summary>
    ///     Average time a receiver takes to unload and receive one trailer
    /// </summary>
    public double MinutesPerTrailer { get; set; } = 45;

    /// <summary>
    ///     Putaway locations are <c>{PutawayLocationPrefix}01</c> through
    ///     <c>{PutawayLocationCount}</c>, matching <c>db/seed/02_locations.sql</c>
    /// </summary>
    public string PutawayLocationPrefix { get; set; } = "";

    public int PutawayLocationCount { get; set; }

    /// <summary>
    ///     Carriers that deliver to the facility; each must exist in Throughline's carrier master
    /// </summary>
    public string[] CarrierScacs { get; set; } = [];

    /// <summary>
    ///     Test tenant the simulated staff belong to
    /// </summary>
    public string UserTenantId { get; set; } = "";

    /// <summary>
    ///     Public client the simulated staff sign in through (password grant)
    /// </summary>
    public string UserClientId { get; set; } = "";

    public List<StaffOptions> Receivers { get; set; } = [];

    public RetryProfile Retry { get; set; } = new();
}

public sealed class ShiftOptions
{
    public DayOfWeek[] Days { get; set; } = [];

    public TimeSpan Start { get; set; }

    public TimeSpan End { get; set; }
}

/// <summary>
///     A simulated member of staff; a user in the test tenant
/// </summary>
public sealed class StaffOptions
{
    public string Username { get; set; } = "";

    public string Password { get; set; } = "";
}
