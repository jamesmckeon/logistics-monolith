using JasperFx.Core;
using JasperFx.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Throughline.Api;
using Throughline.Common.Events;
using Throughline.Modules.Inventory.Presentation;
using Throughline.Modules.Ordering.Presentation;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(
            "throughline-api",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    })
    .WithLogging(
        logging => logging.AddOtlpExporter(),
        options =>
        {
            // Ships the rendered message and scope attributes so Aspire's
            // Structured logs view isn't just empty templates.
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });


builder.Services.AddOpenApi();

var cs = builder.Configuration.GetConnectionString("Throughline");

if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException(
        "Connection string 'Throughline' is missing or empty. " +
        "Set ConnectionStrings:Throughline in configuration.");

builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithPostgresql(cs, "wolverine");

    // EF Core transactional middleware: [Transactional] handlers commit their DbContext changes
    // and the incoming message's Handled status in one transaction.
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableLocalQueues();

    // Poison messages (permanent/contract-violating failures) skip retries and go straight
    // to the dead-letter queue. Transient faults are left to throw normally so they retry.
    opts.OnException<UnrecoverableMessageException>().MoveToErrorQueue();

    // A referenced dependency isn't present locally yet — very likely an eventual-consistency
    // gap (the reference-data event hasn't arrived). Schedule durable, delayed retries so the
    // reference has time to replicate; dead-letter only after the backoff is exhausted.
    opts.OnException<MissingDependencyException>()
        .ScheduleRetry(1.Minutes(), 5.Minutes(), 15.Minutes())
        .Then.MoveToErrorQueue();

    // Discover message handlers in the module assemblies;
    // Wolverine only scans the entry assembly by default.
    opts.Discovery.IncludeAssembly(typeof(InventoryExtensions).Assembly);
});

// dev convenience — provisions the "wolverine" tables on boot:
if (builder.Environment.IsDevelopment())
    builder.Host.UseResourceSetupOnStartup();

builder.Services.AddOrdering(builder.Configuration);
builder.Services.AddInventory(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapOrdering();
app.MapInventory();

app.Run();