using MobileMoney.Production.Correlation;
using MobileMoney.Production.Diagnostics;
using MobileMoney.Production.Errors;
using MobileMoney.Production.Extensions;
using MobileMoney.Production.Health;
using MobileMoney.Production.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMtnMomoProductionConfiguration(builder.Configuration);
builder.Services.AddSingleton<StructuredOperationLogger>();

if (builder.Environment.IsEnvironment("Staging") || builder.Environment.IsEnvironment("Production"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}
else
{
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<MobileMoneyExceptionMiddleware>();

app.MapGet("/health/live", async (HealthProbeRegistry registry, CancellationToken cancellationToken) =>
{
    var response = await registry.RunAsync(probe => probe.Name is "mtn-momo-configuration" or "mtn-momo-secret-provider" or "mtn-momo-connector", cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/health/ready", async (HealthProbeRegistry registry, CancellationToken cancellationToken) =>
{
    var response = await registry.RunAsync(probe => probe.Name is "mtn-momo-configuration" or "mtn-momo-secret-provider" or "mtn-momo-connector" or "mtn-momo-readiness", cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/health/startup", async (HealthProbeRegistry registry, CancellationToken cancellationToken) =>
{
    var response = await registry.RunAsync(probe => probe.Name is "mtn-momo-configuration" or "mtn-momo-secret-provider" or "mtn-momo-connector", cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/internal/configuration/diagnostics", (ConfigurationDiagnosticService diagnostics) =>
{
    return Results.Ok(diagnostics.GetDiagnosticSnapshot());
});

app.MapGet("/internal/logging/echo", (HttpContext httpContext, StructuredOperationLogger logger) =>
{
    var correlation = CorrelationContext.FromHttpContext(httpContext) ?? new CorrelationContext(CorrelationIdValidator.Generate());
    using var scope = logger.BeginScope(new MobileMoneyLoggingScope(correlation));
    logger.LogRequestAccepted(MobileMoneyLogEvents.RequestAccepted, correlation, 15000, "XAF", "237670000000");
    return Results.Ok(new { correlationId = correlation.CorrelationId });
});

app.Run();
