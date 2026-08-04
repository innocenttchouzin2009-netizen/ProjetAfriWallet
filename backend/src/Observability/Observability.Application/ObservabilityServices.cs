using Observability.Api.Domain;

namespace Observability.Api.Application;

public sealed class StructuredLogger
{
    public string Log(string eventName, object payload)
    {
        var entry = new StructuredLogEntry
        {
            Event = eventName,
            PaymentIntentId = payload.GetType().GetProperty("paymentIntentId")?.GetValue(payload)?.ToString(),
            WalletId = payload.GetType().GetProperty("walletId")?.GetValue(payload)?.ToString(),
            Awid = payload.GetType().GetProperty("awid")?.GetValue(payload)?.ToString(),
            DurationMs = payload.GetType().GetProperty("durationMs")?.GetValue(payload) is int value ? value : null,
            Result = payload.GetType().GetProperty("result")?.GetValue(payload)?.ToString(),
            CorrelationId = Environment.GetEnvironmentVariable("CORRELATION_ID")
        };

        return System.Text.Json.JsonSerializer.Serialize(entry);
    }
}

public sealed class CorrelationContext
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public string Create(string requestId)
    {
        var correlationId = $"{requestId}-{Guid.NewGuid():N}";
        _values[requestId] = correlationId;
        Environment.SetEnvironmentVariable("CORRELATION_ID", correlationId);
        return correlationId;
    }

    public string Propagate(string correlationId, string component)
    {
        _values[component] = correlationId;
        Environment.SetEnvironmentVariable("CORRELATION_ID", correlationId);
        return correlationId;
    }
}

public sealed class HealthCheckService
{
    public Dictionary<string, string> GetStatus() => new()
    {
        ["/health"] = "ok",
        ["/health/live"] = "ok",
        ["/health/ready"] = "ok"
    };
}

public sealed class AuditService
{
    private readonly List<AuditEvent> _events = new();

    public void RecordCritical(string code, string service, string correlationId)
    {
        _events.Add(new AuditEvent { Code = code, Service = service, CorrelationId = correlationId });
    }

    public IReadOnlyList<AuditEvent> List() => _events.AsReadOnly();
}

public sealed class TelemetryCollector
{
    private readonly Dictionary<string, double> _metrics = new(StringComparer.OrdinalIgnoreCase);

    public void Record(string name, double value)
    {
        _metrics[name] = _metrics.TryGetValue(name, out var current) ? current + value : value;
    }

    public IReadOnlyDictionary<string, double> Snapshot() => _metrics;
}
