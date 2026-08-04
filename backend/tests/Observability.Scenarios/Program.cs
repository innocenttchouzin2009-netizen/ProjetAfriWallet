using Observability.Api.Application;
using Observability.Api.Domain;

var logger = new StructuredLogger();
var correlation = new CorrelationContext();
var health = new HealthCheckService();
var audit = new AuditService();
var telemetry = new TelemetryCollector();

var failures = new List<string>();

Run("structured log emitted", () =>
{
    var entry = logger.Log("PaymentAuthorized", new { paymentIntentId = "pi-1", walletId = "w-1", awid = "a-1", durationMs = 42, result = "SUCCESS" });
    Assert(entry.Contains("PaymentAuthorized"), "structured log should include event name");
    Assert(entry.Contains("pi-1"), "structured log should include payment intent id");
});

Run("correlation id propagated", () =>
{
    var id = correlation.Create("req-1");
    var propagated = correlation.Propagate(id, "wallet");
    Assert(propagated == id, "correlation id should be preserved");
});

Run("health status reports", () =>
{
    var status = health.GetStatus();
    Assert(status.ContainsKey("/health"), "health should expose /health");
    Assert(status["/health"] == "ok", "default health route should be ok");
});

Run("audit records critical errors", () =>
{
    audit.RecordCritical("Unhandled Exception", "svc-1", "request-1");
    var entries = audit.List();
    Assert(entries.Count >= 1, "audit should capture critical errors");
    Assert(entries.Any(entry => entry.Code == "Unhandled Exception"), "audit should include the supplied code");
});

Run("telemetry metrics exposed", () =>
{
    telemetry.Record("wallet_requests_total", 1);
    telemetry.Record("payment_authorized_total", 1);
    telemetry.Record("fraud_assessment_total", 1);
    var metrics = telemetry.Snapshot();
    Assert(metrics.ContainsKey("wallet_requests_total"), "wallet metric should be recorded");
    Assert(metrics["payment_authorized_total"] >= 1, "payment metric should be recorded");
});

if (failures.Count > 0)
{
    Console.WriteLine("Observability scenarios failed:");
    foreach (var failure in failures)
    {
        Console.WriteLine($"[FAIL] {failure}");
    }
    Environment.Exit(1);
}

Console.WriteLine("All AFW-0005.8.2 observability scenarios passed.");

void Run(string name, Action action)
{
    try
    {
        action();
        Console.WriteLine($"[OK] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[FAIL] {name}: {ex.Message}");
    }
}

void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
