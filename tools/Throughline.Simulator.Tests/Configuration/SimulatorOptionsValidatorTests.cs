using Throughline.Simulator.Configuration;

namespace Throughline.Simulator.Tests.Configuration;

[Category("Unit")]
public sealed class SimulatorOptionsValidatorTests
{
    private SimulatorOptionsValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new SimulatorOptionsValidator();
    }

    #region Validate

    [Test]
    public void Validate_CompleteOptions_Succeeds()
    {
        var options = ValidOptions(ValidProfile());

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Succeeded, Is.True);
    }

    [Test]
    public void Validate_Disabled_SucceedsWithoutCheckingAnything()
    {
        var options = new SimulatorOptions { Enabled = false };

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Succeeded, Is.True);
    }

    [Test]
    public void Validate_TwentyThreeHourlyWeights_FailsNamingHourlyWeights()
    {
        var profile = ValidProfile();
        profile.HourlyWeights = Enumerable.Repeat(1.0, 23).ToArray();

        var actual = _sut.Validate(null, ValidOptions(profile));

        Assert.That(actual.Failures, Has.Some.Contains("HourlyWeights must have 24 values"));
    }

    [Test]
    public void Validate_FaultRatesAddUpToMoreThanOne_FailsNamingFaults()
    {
        var profile = ValidProfile();
        profile.Faults = new FaultRates { UnknownSku = 0.6, MissingRequiredField = 0.5 };

        var actual = _sut.Validate(null, ValidOptions(profile));

        Assert.That(actual.Failures, Has.Some.Contains("Faults rates must not add up to more than 1"));
    }

    [Test]
    public void Validate_UnknownTimeZone_FailsNamingTimeZone()
    {
        var profile = ValidProfile();
        profile.TimeZone = "America/Atlantis";

        var actual = _sut.Validate(null, ValidOptions(profile));

        Assert.That(actual.Failures, Has.Some.Contains("TimeZone 'America/Atlantis' is not a known time zone"));
    }

    [Test]
    public void Validate_MissingSecretAndUrl_ReportsEveryProblem()
    {
        var profile = ValidProfile();
        profile.OmsClientSecret = "";
        var options = ValidOptions(profile);
        options.ThroughlineBaseUrl = null;

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Failures, Has.Some.Contains("OmsClientSecret is required")
            .And.Some.Contains("ThroughlineBaseUrl must be an absolute URL"));
    }

    [Test]
    public void Validate_NoCatalogConnectionString_FailsNamingIt()
    {
        var options = ValidOptions(ValidProfile());
        options.CatalogConnectionString = "";

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Failures, Has.Some.Contains("CatalogConnectionString is required"));
    }

    [Test]
    public void Validate_BuyingWithoutLedger_FailsNamingLedger()
    {
        var profile = ValidProfile();
        profile.Buying = new BuyingProfile { Enabled = true, SupplierName = "Ridgeline Apparel Manufacturing" };

        var actual = _sut.Validate(null, ValidOptions(profile));

        Assert.That(actual.Failures, Has.Some.Contains("LedgerConnectionString is required"));
    }

    [Test]
    public void Validate_ReceiversWithoutSignInClient_FailsNamingUserClientId()
    {
        var options = ValidOptions(ValidProfile());
        options.LedgerConnectionString = "Host=localhost;Database=simulator";
        options.Facility = ValidFacility();
        options.Facility.UserClientId = "";

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Failures, Has.Some.Contains("UserClientId is required"));
    }

    [Test]
    public void Validate_CompleteFacilityWithReceivers_Succeeds()
    {
        var options = ValidOptions(ValidProfile());
        options.LedgerConnectionString = "Host=localhost;Database=simulator";
        options.Facility = ValidFacility();

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Succeeded, Is.True);
    }

    [Test]
    public void Validate_CompleteFacilityWithAllocatorsOnly_SucceedsWithoutLedger()
    {
        var profile = ValidProfile();
        profile.Allocation = ValidAllocation();
        var options = ValidOptions(profile);
        options.Facility = ValidAllocatingFacility();

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Succeeded, Is.True);
    }

    [Test]
    public void Validate_AllocatorsShareDockShift_FailsNamingDockShift()
    {
        var options = ValidOptions(ValidProfile());
        options.Facility = ValidAllocatingFacility();
        options.Facility.Allocators.Add(
            new AllocatorOptions { Username = "allocator2@example.com", Password = "password", DockShifts = [0] });

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Failures, Has.Some.Contains("Allocators must not share a dock shift"));
    }

    [Test]
    public void Validate_AllocatorDockShiftOutOfRange_FailsNamingDockShifts()
    {
        var options = ValidOptions(ValidProfile());
        options.Facility = ValidAllocatingFacility();
        options.Facility.Allocators[0].DockShifts = [1];

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Failures, Has.Some.Contains("Allocators each need DockShifts"));
    }

    [Test]
    public void Validate_AllocatorUsernameSameAsReceivers_FailsNamingUsernames()
    {
        var options = ValidOptions(ValidProfile());
        options.LedgerConnectionString = "Host=localhost;Database=simulator";
        options.Facility = ValidFacility();
        options.Facility.Allocators =
            [new AllocatorOptions { Username = "receiver1@example.com", Password = "password", DockShifts = [0] }];

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Failures, Has.Some.Contains("Allocators must have different usernames"));
    }

    [Test]
    public void Validate_AllocationWithoutWaves_FailsNamingWaves()
    {
        var profile = ValidProfile();
        profile.Allocation = new AllocationProfile { Enabled = true };
        var options = ValidOptions(profile);
        options.Facility = ValidAllocatingFacility();

        var actual = _sut.Validate(null, options);

        Assert.That(actual.Failures, Has.Some.Contains("Allocation:Waves must contain at least one wave"));
    }

    [Test]
    public void Validate_AllocationWithoutWavesButNoAllocators_Succeeds()
    {
        var profile = ValidProfile();
        profile.Allocation = new AllocationProfile { Enabled = true };

        var actual = _sut.Validate(null, ValidOptions(profile));

        Assert.That(actual.Succeeded, Is.True);
    }

    #endregion

    private static FacilityOptions ValidAllocatingFacility() => new()
    {
        TimeZone = "America/Chicago",
        DockShifts =
            [new ShiftOptions { Days = [DayOfWeek.Monday], Start = TimeSpan.FromHours(6), End = TimeSpan.FromHours(14.5) }],
        UserTenantId = "test-tenant",
        UserClientId = "sign-in-client",
        Allocators = [new AllocatorOptions { Username = "allocator1@example.com", Password = "password", DockShifts = [0] }]
    };

    private static AllocationProfile ValidAllocation() => new()
    {
        Enabled = true,
        SameDayCutoff = TimeSpan.FromHours(14),
        Waves = [new WaveTimesOptions { Days = [DayOfWeek.Monday], Times = [TimeSpan.FromHours(6.5)] }]
    };

    private static FacilityOptions ValidFacility() => new()
    {
        TimeZone = "America/Chicago",
        DockShifts =
            [new ShiftOptions { Days = [DayOfWeek.Monday], Start = TimeSpan.FromHours(6), End = TimeSpan.FromHours(14.5) }],
        PutawayLocationPrefix = "BULK-",
        PutawayLocationCount = 40,
        CarrierScacs = ["ODFL"],
        UserTenantId = "test-tenant",
        UserClientId = "sign-in-client",
        Receivers = [new StaffOptions { Username = "receiver1@example.com", Password = "password" }]
    };

    private static SimulatorOptions ValidOptions(CustomerProfile profile) => new()
    {
        ThroughlineBaseUrl = new Uri("https://throughline.example.com"),
        TenantId = "tenant",
        ApiScope = "api://throughline/.default",
        CatalogConnectionString = "Host=localhost;Database=throughline",
        Customers = new Dictionary<string, CustomerProfile> { ["HarrowFinch"] = profile }
    };

    private static CustomerProfile ValidProfile() => new()
    {
        OwnerId = 1,
        ReferencePrefix = "HFO",
        TimeZone = "America/Chicago",
        OrdersPerDay = 600,
        HourlyWeights = Enumerable.Repeat(1.0, 24).ToArray(),
        WeekdayWeights = Enumerable.Repeat(1.0, 7).ToArray(),
        LinesPerOrder = new SizeProfile { P50 = 2, P95 = 6 },
        QuantityPerLine = new SizeProfile { P50 = 1, P95 = 4 },
        Faults = new FaultRates { IdenticalResend = 0.02 },
        OmsClientId = "client",
        OmsClientSecret = "secret"
    };
}
