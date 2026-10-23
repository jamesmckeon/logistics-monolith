using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using Throughline.Simulator.Allocation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Throughline.Simulator.Buying;
using Throughline.Simulator.Configuration;
using Throughline.Simulator.Http;
using Throughline.Simulator.Identity;
using Throughline.Simulator.Ledger;
using Throughline.Simulator.Oms;
using Throughline.Simulator.Orders;
using Throughline.Simulator.Receiving;
using Throughline.Simulator.Telemetry;

var builder = Host.CreateApplicationBuilder(args);

var section = builder.Configuration.GetSection(SimulatorOptions.SectionName);

builder.Services.AddOptions<SimulatorOptions>()
    .Bind(section)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<SimulatorOptions>, SimulatorOptionsValidator>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SimulatorMetrics>();
builder.Services.AddSingleton<ISkuCatalogSource, PostgresSkuCatalogSource>();
builder.Services.AddSingleton<ILedger, PostgresLedger>();
builder.Services.AddSingleton<TokenSources>();
builder.Services.AddHttpClient(TokenSources.EntraHttpClient).AddStandardResilienceHandler();

// registered even when no customer client is, so a bad configuration reaches ValidateOnStart's
// message instead of failing to build the worker
builder.Services.AddHttpClient();

// The HTTP clients are registered from configuration read now; ValidateOnStart reports a bad
// configuration before any client is used
var simulator = section.Get<SimulatorOptions>() ?? new SimulatorOptions();

if (simulator is { Enabled: true, ThroughlineBaseUrl: { IsAbsoluteUri: true } throughline })
{
    foreach (var (customer, profile) in simulator.Customers.Where(c => c.Value.Enabled))
    {
        builder.Services.AddThroughlineClient(
            ThroughlineHttpClients.ForCustomer(customer), throughline, profile.Retry,
            tokens => tokens.ForCustomer(customer));
    }

    var staff = simulator.Facility.Receivers.Select(r => r.Username)
        .Concat(simulator.Facility.Allocators.Select(a => a.Username));

    foreach (var username in staff)
    {
        builder.Services.AddThroughlineClient(
            ThroughlineHttpClients.ForStaff(username), throughline, simulator.Facility.Retry,
            tokens => tokens.ForStaff(username));
    }
}

builder.Services.AddHostedService<OmsWorker>();
builder.Services.AddHostedService<BuyerWorker>();
builder.Services.AddHostedService<ReceiverWorker>();
builder.Services.AddHostedService<AllocatorWorker>();

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
