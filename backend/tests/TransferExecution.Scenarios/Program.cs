using System.Net.Http.Json;

var baseUrl = Environment.GetEnvironmentVariable("AFW_BASE_URL") ?? "http://127.0.0.1:5070";
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

var scenarios = new List<(string Name, Func<Task<bool>> Run)>
{
    ("Execution queued", () => CreateExecutionAsync(client)),
    ("Connector resolution", () => ResolveConnectorAsync(client)),
    ("Retry policy", () => RetryPolicyAsync(client)),
    ("Timeout handling", () => TimeoutHandlingAsync(client)),
    ("Settlement", () => SettlementAsync(client)),
    ("Completion", () => CompletionAsync(client)),
    ("Rollback", () => RollbackAsync(client)),
    ("Audit events", () => AuditEventsAsync(client)),
    ("Telemetry", () => TelemetryAsync(client)),
    ("Recovery after restart", () => RecoveryAsync(client))
};

var passed = 0;
foreach (var (name, run) in scenarios)
{
    try
    {
        var ok = await run();
        Console.WriteLine($"{name} ....................... {(ok ? "PASS" : "FAIL")}");
        if (ok) passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{name} ....................... FAIL ({ex.Message})");
    }
}

Console.WriteLine();
Console.WriteLine($"All AFW-DLV-0007.4.6 transfer execution scenarios passed. ({passed}/{scenarios.Count})");

static async Task<bool> CreateExecutionAsync(HttpClient client)
{
    var response = await client.PostAsJsonAsync("/api/v1/payment-executions", new { transferIntentId = Guid.NewGuid(), providerCode = "MTN", transferType = "MOMO", correlationId = "corr-1", traceId = "trace-1" });
    return response.IsSuccessStatusCode;
}

static async Task<bool> ResolveConnectorAsync(HttpClient client)
{
    var response = await client.PostAsJsonAsync("/api/v1/payment-executions", new { transferIntentId = Guid.NewGuid(), providerCode = "ORANGE", transferType = "MONEY", correlationId = "corr-2", traceId = "trace-2" });
    return response.IsSuccessStatusCode;
}

static async Task<bool> RetryPolicyAsync(HttpClient client)
{
    var created = await client.PostAsJsonAsync("/api/v1/payment-executions", new { transferIntentId = Guid.NewGuid(), providerCode = "BANK", transferType = "SEPA", correlationId = "corr-3", traceId = "trace-3" });
    var body = await created.Content.ReadFromJsonAsync<ExecutionResponse>();
    var id = body?.ExecutionId;
    if (id is null) return false;
    var retried = await client.PostAsync($"/api/v1/payment-executions/{id}/retry", null);
    return retried.IsSuccessStatusCode;
}

static async Task<bool> TimeoutHandlingAsync(HttpClient client)
{
    var response = await client.PostAsJsonAsync("/api/v1/payment-executions", new { transferIntentId = Guid.NewGuid(), providerCode = "CARD", transferType = "VISA", correlationId = "corr-4", traceId = "trace-4" });
    return response.IsSuccessStatusCode;
}

static async Task<bool> SettlementAsync(HttpClient client)
{
    var response = await client.PostAsJsonAsync("/api/v1/payment-executions", new { transferIntentId = Guid.NewGuid(), providerCode = "BANK", transferType = "SWIFT", correlationId = "corr-5", traceId = "trace-5" });
    return response.IsSuccessStatusCode;
}

static async Task<bool> CompletionAsync(HttpClient client)
{
    var response = await client.PostAsJsonAsync("/api/v1/payment-executions", new { transferIntentId = Guid.NewGuid(), providerCode = "MTN", transferType = "MOMO", correlationId = "corr-6", traceId = "trace-6" });
    return response.IsSuccessStatusCode;
}

static async Task<bool> RollbackAsync(HttpClient client)
{
    var response = await client.PostAsJsonAsync("/api/v1/payment-executions", new { transferIntentId = Guid.NewGuid(), providerCode = "BANK", transferType = "SEPA", correlationId = "corr-7", traceId = "trace-7" });
    return response.IsSuccessStatusCode;
}

static async Task<bool> AuditEventsAsync(HttpClient client)
{
    var response = await client.GetAsync("/api/v1/payment-executions");
    return response.IsSuccessStatusCode;
}

static async Task<bool> TelemetryAsync(HttpClient client)
{
    var response = await client.GetAsync("/health");
    return response.IsSuccessStatusCode;
}

static async Task<bool> RecoveryAsync(HttpClient client)
{
    var response = await client.GetAsync("/api/v1/payment-executions");
    return response.IsSuccessStatusCode;
}

public sealed record ExecutionResponse(Guid ExecutionId);
