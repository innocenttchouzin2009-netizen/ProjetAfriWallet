using Observability.Api.Application;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<StructuredLogger>();
builder.Services.AddSingleton<CorrelationContext>();
builder.Services.AddSingleton<HealthCheckService>();
builder.Services.AddSingleton<AuditService>();
builder.Services.AddSingleton<TelemetryCollector>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.MapPost("/api/v1/observability/logs", (StructuredLogger logger) =>
{
    var payload = new { paymentIntentId = "pi-123", walletId = "wallet-123", awid = "awid-123", durationMs = 42, result = "SUCCESS" };
    var output = logger.Log("PaymentAuthorized", payload);
    return Results.Ok(new { message = output });
});

app.MapGet("/api/v1/observability/metrics", (TelemetryCollector collector) =>
{
    collector.Record("wallet_requests_total", 1);
    collector.Record("payment_authorized_total", 1);
    collector.Record("fraud_assessment_total", 1);
    return Results.Ok(collector.Snapshot());
});

app.Run();
