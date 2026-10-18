using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Oms;
using Throughline.Simulator.Telemetry;

var builder = Host.CreateApplicationBuilder(args);

var section = builder.Configuration.GetSection(SimulatorOptions.SectionName);

builder.Services.AddOptions<SimulatorOptions>()
    .Bind(section)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<SimulatorOptions>, SimulatorOptionsValidator>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SimulatorMetrics>();

// registered even when no customer client is, so a bad configuration reaches ValidateOnStart's
// message instead of failing to build the worker
builder.Services.AddHttpClient();

// The HTTP clients are registered from configuration read now; ValidateOnStart reports a bad
// configuration before any client is used
var simulator = section.Get<SimulatorOptions>() ?? new SimulatorOptions();

if (simulator is { Enabled: true, ThroughlineBaseUrl.IsAbsoluteUri: true })
{
    foreach (var (customer, profile) in simulator.Customers.Where(c => c.Value.Enabled))
        builder.Services.AddOmsHttpClient(customer, profile, simulator);
}

builder.Services.AddHostedService<OmsWorker>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("throughline-simulator", serviceVersion: "1.0.0"))
    .WithMetrics(metrics => metrics
        .AddMeter(SimulatorMetrics.MeterName)
        .AddMeter("System.Net.Http")
        .AddOtlpExporter())
    .WithLogging(
        logging => logging.AddOtlpExporter(),
        options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

builder.Build().Run();
