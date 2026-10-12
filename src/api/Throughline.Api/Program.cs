using JasperFx.Core;
using JasperFx.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Throughline.Api;
using Throughline.Api.Auth;
using Throughline.Common.Events;
using Throughline.Common.Presentation.Http;
using Throughline.Modules.Inventory.Presentation;
using Throughline.Modules.Ordering.Presentation;
using Throughline.Modules.Receiving.Presentation;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddThroughlineAuthorization();

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
{
    throw new InvalidOperationException(
        "Connection string 'Throughline' is missing or empty. " +
        "Set ConnectionStrings:Throughline in configuration.");
}

builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithPostgresql(cs, "wolverine");

    // EF Core transactional middleware: [Transactional] handlers commit their DbContext changes
    // and the incoming message's Handled status in one transaction.
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableLocalQueues();

    // Each handler of a message type gets its own subscription and copy of the message, so modules handling the
    // same integration event succeed, retry and dead-letter independently. 
    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

    // Poison messages (permanent/contract-violating failures) skip retries and go straight
    // to the dead-letter queue. Transient faults are left to throw normally so they retry.
    opts.OnException<UnrecoverableMessageException>().MoveToErrorQueue();

    // Schedules retries when an event handler throws an exception due to a message
    // containing lookup/reference data that doesn't exist in the module's local data store
    opts.OnException<MissingDependencyException>()
        .ScheduleRetry(1.Minutes(), 5.Minutes(), 15.Minutes())
        .Then.MoveToErrorQueue();

    // Discover message handlers in the module assemblies;
    // Wolverine only scans the entry assembly by default.
    opts.Discovery.IncludeAssembly(typeof(InventoryExtensions).Assembly);
});

// dev convenience — provisions the "wolverine" tables on boot:
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseResourceSetupOnStartup();
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<IRequestContext, RequestContext>();

builder.Services.AddOrdering(builder.Configuration);
builder.Services.AddInventory(builder.Configuration);
builder.Services.AddReceiving(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapOrdering();
app.MapInventory();
app.MapReceiving();

app.Run();