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

    #endregion

    private static SimulatorOptions ValidOptions(CustomerProfile profile) => new()
    {
        ThroughlineBaseUrl = new Uri("https://throughline.example.com"),
        TenantId = "tenant",
        ApiScope = "api://throughline/.default",
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
        Catalog = new CatalogProfile { GeneratedSkuPrefix = "HFO-", GeneratedSkuCount = 100 },
        Faults = new FaultRates { IdenticalResend = 0.02 },
        OmsClientId = "client",
        OmsClientSecret = "secret"
    };
}
