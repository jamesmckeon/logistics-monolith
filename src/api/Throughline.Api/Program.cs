using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Throughline.Api;
using Throughline.Modules.Ordering.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(
            "orders-api",
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

builder.Services.AddOrdering(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapOrdering();

app.Run();