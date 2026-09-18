using JasperFx.Resources;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Throughline.Api;
using Throughline.Modules.Ordering.Presentation;
using Wolverine;
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
    opts.Policies.UseDurableLocalQueues();
});

// dev convenience — provisions the "wolverine" tables on boot:
if (builder.Environment.IsDevelopment())
    builder.Host.UseResourceSetupOnStartup();

builder.Services.AddOrdering(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapOrdering();

app.Run();